#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Services.Security;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace egibi_api.Services
{
    public class FundingService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;

        public FundingService(EgibiDbContext db, IEncryptionService encryption)
        {
            _db = db;
            _encryption = encryption;
        }

        // =============================================
        // GET PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Returns the user's primary funding source, or null if none is configured.
        /// </summary>
        public async Task<RequestResponse> GetPrimaryFundingSource(int appUserId)
        {
            var account = await _db.Accounts
                .Include(a => a.Connection)
                .FirstOrDefaultAsync(a => a.AppUserId == appUserId && a.IsPrimaryFunding);

            if (account == null)
                return new RequestResponse(null, 200, "No primary funding source configured");

            // Load credential summary
            UserCredential credential = null;
            if (account.ConnectionId.HasValue)
            {
                credential = await _db.UserCredentials
                    .FirstOrDefaultAsync(uc => uc.AppUserId == appUserId && uc.ConnectionId == account.ConnectionId.Value);
            }

            var response = new FundingSourceResponse
            {
                AccountId = account.Id,
                AccountName = account.Name,
                Description = account.Description ?? "",
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,

                ConnectionId = account.ConnectionId ?? 0,
                ProviderName = account.Connection?.Name ?? "",
                ProviderIconKey = account.Connection?.IconKey ?? "",
                ProviderColor = account.Connection?.Color ?? "",
                ProviderWebsite = account.Connection?.Website ?? "",
                BaseUrl = account.Connection?.DefaultBaseUrl ?? "",
                LinkMethod = account.Connection?.LinkMethod ?? "api_key",

                HasCredentials = credential != null,
                CredentialLabel = credential?.Label ?? "",
                MaskedApiKey = MaskValue(credential?.EncryptedApiKey, appUserId),
                CredentialLastUsedAt = credential?.LastUsedAt,
            };

            // If this is a Plaid-linked source, load Plaid details
            if (account.Connection?.LinkMethod == "plaid_link")
            {
                var plaidItem = await _db.PlaidItems
                    .Include(pi => pi.PlaidAccounts)
                    .FirstOrDefaultAsync(pi => pi.AccountId == account.Id && pi.AppUserId == appUserId);

                if (plaidItem != null)
                {
                    var selectedAccount = plaidItem.PlaidAccounts?.FirstOrDefault(a => a.IsSelectedFunding)
                        ?? plaidItem.PlaidAccounts?.FirstOrDefault();

                    response.HasCredentials = true;
                    response.PlaidDetails = new PlaidFundingDetails
                    {
                        PlaidItemId = plaidItem.Id,
                        InstitutionId = plaidItem.InstitutionId,
                        InstitutionName = plaidItem.InstitutionName,
                        LastSyncedAt = plaidItem.LastSyncedAt,
                        PlaidAccountId = selectedAccount?.PlaidAccountId,
                        AccountName = selectedAccount?.OfficialName ?? selectedAccount?.Name,
                        Mask = selectedAccount?.Mask,
                        AccountType = selectedAccount?.AccountType,
                        AccountSubtype = selectedAccount?.AccountSubtype,
                        AvailableBalance = selectedAccount?.AvailableBalance,
                        CurrentBalance = selectedAccount?.CurrentBalance,
                        IsoCurrencyCode = selectedAccount?.IsoCurrencyCode,
                        BalanceLastUpdatedAt = selectedAccount?.BalanceLastUpdatedAt,
                    };
                }
            }

            return new RequestResponse(response, 200, "OK");
        }

        // =============================================
        // GET FUNDING PROVIDERS
        // =============================================

        /// <summary>
        /// Returns the list of available funding provider connections.
        /// </summary>
        public async Task<RequestResponse> GetFundingProviders()
        {
            var providers = await _db.Connections
                .Where(c => c.Category == "funding_provider" && c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new FundingProviderEntry
                {
                    ConnectionId = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IconKey = c.IconKey,
                    Color = c.Color,
                    Website = c.Website,
                    DefaultBaseUrl = c.DefaultBaseUrl,
                    RequiredFields = ParseRequiredFields(c.RequiredFields),
                    SignupUrl = GetSignupUrl(c.IconKey),
                    ApiDocsUrl = GetApiDocsUrl(c.IconKey),
                    LinkMethod = c.LinkMethod ?? "api_key",
                })
                .ToListAsync();

            return new RequestResponse(providers, 200, "OK");
        }

        // =============================================
        // CREATE / SET PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Creates a new account marked as primary funding source.
        /// If a previous primary funding source exists, it is demoted.
        /// </summary>
        public async Task<RequestResponse> SetPrimaryFundingSource(CreateFundingSourceRequest request, int appUserId)
        {
            // Verify the connection exists and is a funding provider
            var connection = await _db.Connections
                .FirstOrDefaultAsync(c => c.Id == request.ConnectionId && c.Category == "funding_provider" && c.IsActive);

            if (connection == null)
                return new RequestResponse(null, 400, "Invalid funding provider");

            // Get user for encryption
            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null)
                return new RequestResponse(null, 401, "User not found");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Demote any existing primary funding source
                var existingPrimary = await _db.Accounts
                    .Where(a => a.AppUserId == appUserId && a.IsPrimaryFunding)
                    .ToListAsync();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimaryFunding = false;
                    existing.LastModifiedAt = DateTime.UtcNow;
                }

                // Create the new account
                var account = new Account
                {
                    Name = request.Name,
                    Description = request.Description ?? $"{connection.Name} funding account",
                    ConnectionId = connection.Id,
                    AppUserId = appUserId,
                    IsPrimaryFunding = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.Accounts.Add(account);
                await _db.SaveChangesAsync();

                // Encrypt and store credentials if provided
                if (request.Credentials != null)
                {
                    // Remove existing credential for this user + connection
                    var existingCred = await _db.UserCredentials
                        .FirstOrDefaultAsync(uc => uc.AppUserId == appUserId && uc.ConnectionId == connection.Id);

                    if (existingCred != null)
                        _db.UserCredentials.Remove(existingCred);

                    var credential = new UserCredential
                    {
                        AppUserId = appUserId,
                        ConnectionId = connection.Id,
                        Label = request.Credentials.Label ?? $"{connection.Name} API Key",
                        KeyVersion = 1,
                        Name = $"{connection.Name} Credential",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    // Encrypt each provided field
                    if (!string.IsNullOrWhiteSpace(request.Credentials.ApiKey))
                        credential.EncryptedApiKey = _encryption.Encrypt(request.Credentials.ApiKey, appUser.EncryptedDataKey);

                    if (!string.IsNullOrWhiteSpace(request.Credentials.ApiSecret))
                        credential.EncryptedApiSecret = _encryption.Encrypt(request.Credentials.ApiSecret, appUser.EncryptedDataKey);

                    if (!string.IsNullOrWhiteSpace(request.Credentials.Passphrase))
                        credential.EncryptedPassphrase = _encryption.Encrypt(request.Credentials.Passphrase, appUser.EncryptedDataKey);

                    if (!string.IsNullOrWhiteSpace(request.Credentials.Username))
                        credential.EncryptedUsername = _encryption.Encrypt(request.Credentials.Username, appUser.EncryptedDataKey);

                    if (!string.IsNullOrWhiteSpace(request.Credentials.Password))
                        credential.EncryptedPassword = _encryption.Encrypt(request.Credentials.Password, appUser.EncryptedDataKey);

                    _db.UserCredentials.Add(credential);
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Return the new funding source
                return await GetPrimaryFundingSource(appUserId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new RequestResponse(null, 500, "Failed to set funding source", new ResponseError(ex));
            }
        }

        // =============================================
        // REMOVE PRIMARY FUNDING SOURCE
        // =============================================

        /// <summary>
        /// Removes the primary funding flag from the user's funding account.
        /// Does NOT delete the account — just demotes it.
        /// </summary>
        public async Task<RequestResponse> RemovePrimaryFundingSource(int appUserId)
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AppUserId == appUserId && a.IsPrimaryFunding);

            if (account == null)
                return new RequestResponse(null, 404, "No primary funding source found");

            account.IsPrimaryFunding = false;
            account.LastModifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return new RequestResponse(null, 200, "Primary funding source removed");
        }

        // =============================================
        // HELPERS
        // =============================================

        /// <summary>
        /// Returns a masked version of an encrypted API key for display purposes.
        /// Decrypts, then masks all but the last 4 characters.
        /// </summary>
        private string MaskValue(string encryptedValue, int appUserId)
        {
            if (string.IsNullOrEmpty(encryptedValue))
                return "";

            try
            {
                var user = _db.AppUsers.Find(appUserId);
                if (user == null) return "••••••••";

                var decrypted = _encryption.Decrypt(encryptedValue, user.EncryptedDataKey);
                if (decrypted.Length <= 4) return "••••" + decrypted;
                return new string('•', decrypted.Length - 4) + decrypted[^4..];
            }
            catch
            {
                return "••••••••";
            }
        }

        private static string[] ParseRequiredFields(string json)
        {
            try { return JsonSerializer.Deserialize<string[]>(json ?? "[]"); }
            catch { return Array.Empty<string>(); }
        }

        /// <summary>
        /// Returns the signup URL for a given provider.
        /// Extend this as new funding providers are added.
        /// </summary>
        private static string GetSignupUrl(string iconKey)
        {
            return iconKey switch
            {
                "mercury" => "https://mercury.com/signup",
                _ => ""
            };
        }

        /// <summary>
        /// Returns the API documentation URL for a given provider.
        /// </summary>
        private static string GetApiDocsUrl(string iconKey)
        {
            return iconKey switch
            {
                "mercury" => "https://docs.mercury.com/reference/getting-started-with-your-api",
                _ => ""
            };
        }
    }
}
