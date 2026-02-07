#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Services.Security;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using Account = egibi_api.Data.Entities.Account;

namespace egibi_api.Services
{
    public class AccountsService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountsService(EgibiDbContext db, IEncryptionService encryption, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _encryption = encryption;
            _httpClientFactory = httpClientFactory;
        }

        // =============================================
        // ACCOUNT DETAIL (for detail page)
        // =============================================

        /// <summary>
        /// Returns full account detail with connection metadata, masked credentials, and fees.
        /// Verifies the account belongs to the requesting user.
        /// </summary>
        public async Task<RequestResponse> GetAccountDetail(int accountId, int appUserId)
        {
            try
            {
                var account = await _db.Accounts
                    .Include(a => a.Connection)
                    .Include(a => a.AccountType)
                    .FirstOrDefaultAsync(a => a.Id == accountId && a.AppUserId == appUserId);

                if (account == null)
                    return new RequestResponse(null, 404, "Account not found");

                // Load credentials for this account's connection
                UserCredential credential = null;
                if (account.ConnectionId.HasValue)
                {
                    credential = await _db.UserCredentials
                        .FirstOrDefaultAsync(uc => uc.AppUserId == appUserId && uc.ConnectionId == account.ConnectionId.Value);
                }

                // Load fee structure
                var fees = await _db.AccountFeeStructureDetails
                    .FirstOrDefaultAsync(f => f.AccountId == accountId);

                // Determine base URL (account override or connection default)
                var baseUrl = account.Connection?.DefaultBaseUrl ?? "";

                // Parse required fields from connection
                string[] requiredFields = Array.Empty<string>();
                if (account.Connection?.RequiredFields != null)
                {
                    try { requiredFields = JsonSerializer.Deserialize<string[]>(account.Connection.RequiredFields); }
                    catch { /* malformed JSON — return empty */ }
                }

                var response = new AccountDetailResponse
                {
                    Id = account.Id,
                    Name = account.Name,
                    Description = account.Description ?? "",
                    Notes = account.Notes ?? "",
                    IsActive = account.IsActive,
                    CreatedAt = account.CreatedAt,
                    LastModifiedAt = account.LastModifiedAt,

                    ConnectionId = account.ConnectionId,
                    ConnectionName = account.Connection?.Name ?? "",
                    ConnectionIconKey = account.Connection?.IconKey ?? "",
                    ConnectionColor = account.Connection?.Color ?? "",
                    ConnectionCategory = account.Connection?.Category ?? "",
                    ConnectionWebsite = account.Connection?.Website ?? "",
                    BaseUrl = baseUrl,
                    RequiredFields = requiredFields,

                    AccountTypeId = account.AccountTypeId,
                    AccountTypeName = account.AccountType?.Name ?? "",

                    Credentials = BuildCredentialSummary(credential),

                    Fees = fees != null ? new AccountFeeDetail
                    {
                        Id = fees.Id,
                        MakerFeePercent = fees.MakerFeePercent,
                        TakerFeePercent = fees.TakerFeePercent,
                        FeeScheduleType = fees.FeeScheduleType ?? "flat",
                        Notes = fees.Notes ?? ""
                    } : null
                };

                return new RequestResponse(response, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to load account detail", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates general account properties. Verifies ownership.
        /// </summary>
        public async Task<RequestResponse> UpdateAccount(UpdateAccountRequest request, int appUserId)
        {
            try
            {
                var account = await _db.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.Id && a.AppUserId == appUserId);

                if (account == null)
                    return new RequestResponse(null, 404, "Account not found");

                account.Name = request.Name;
                account.Description = request.Description ?? "";
                account.Notes = request.Notes ?? "";
                account.AccountTypeId = request.AccountTypeId;
                account.IsActive = request.IsActive;
                account.LastModifiedAt = DateTime.UtcNow;

                _db.Update(account);
                await _db.SaveChangesAsync();

                return new RequestResponse(account.Id, 200, "Account updated");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update account", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates encrypted credentials for an account's connection.
        /// Only non-null fields are re-encrypted and updated.
        /// Creates a new UserCredential if one doesn't exist.
        /// </summary>
        public async Task<RequestResponse> UpdateAccountCredentials(UpdateCredentialsRequest request, int appUserId)
        {
            try
            {
                var account = await _db.Accounts
                    .Include(a => a.Connection)
                    .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.AppUserId == appUserId);

                if (account == null)
                    return new RequestResponse(null, 404, "Account not found");

                if (!account.ConnectionId.HasValue)
                    return new RequestResponse(null, 400, "Account has no linked connection");

                var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == appUserId && u.IsActive);
                if (appUser == null)
                    return new RequestResponse(null, 404, "User not found");

                var userKey = appUser.EncryptedDataKey;
                if (string.IsNullOrEmpty(userKey))
                    return new RequestResponse(null, 400, "User encryption key not configured");

                // Update base URL on the account's connection if provided
                if (request.BaseUrl != null && account.Connection != null)
                {
                    account.Connection.DefaultBaseUrl = request.BaseUrl;
                    _db.Update(account.Connection);
                }

                // Find or create credential
                var credential = await _db.UserCredentials
                    .FirstOrDefaultAsync(uc => uc.AppUserId == appUserId && uc.ConnectionId == account.ConnectionId.Value);

                if (credential == null)
                {
                    credential = new UserCredential
                    {
                        AppUserId = appUserId,
                        ConnectionId = account.ConnectionId.Value,
                        Name = $"{account.Name} credentials",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        KeyVersion = 1
                    };
                    await _db.UserCredentials.AddAsync(credential);
                }

                // Update only non-null fields (null = keep existing)
                if (request.ApiKey != null) credential.EncryptedApiKey = EncryptIfPresent(request.ApiKey, userKey);
                if (request.ApiSecret != null) credential.EncryptedApiSecret = EncryptIfPresent(request.ApiSecret, userKey);
                if (request.Passphrase != null) credential.EncryptedPassphrase = EncryptIfPresent(request.Passphrase, userKey);
                if (request.Username != null) credential.EncryptedUsername = EncryptIfPresent(request.Username, userKey);
                if (request.Password != null) credential.EncryptedPassword = EncryptIfPresent(request.Password, userKey);
                if (request.Label != null) credential.Label = request.Label;
                if (request.Permissions != null) credential.Permissions = request.Permissions;

                credential.LastModifiedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return new RequestResponse(BuildCredentialSummary(credential), 200, "Credentials updated");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update credentials", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Updates or creates fee structure for an account.
        /// </summary>
        public async Task<RequestResponse> UpdateAccountFees(UpdateAccountFeesRequest request, int appUserId)
        {
            try
            {
                // Verify account ownership
                var account = await _db.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.AccountId && a.AppUserId == appUserId);

                if (account == null)
                    return new RequestResponse(null, 404, "Account not found");

                var fees = await _db.AccountFeeStructureDetails
                    .FirstOrDefaultAsync(f => f.AccountId == request.AccountId);

                if (fees == null)
                {
                    fees = new AccountFeeStructureDetails
                    {
                        AccountId = request.AccountId,
                        Name = $"{account.Name} fees",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _db.AccountFeeStructureDetails.AddAsync(fees);
                }

                fees.MakerFeePercent = request.MakerFeePercent;
                fees.TakerFeePercent = request.TakerFeePercent;
                fees.FeeScheduleType = request.FeeScheduleType ?? "flat";
                fees.Notes = request.Notes ?? "";
                fees.LastModifiedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var response = new AccountFeeDetail
                {
                    Id = fees.Id,
                    MakerFeePercent = fees.MakerFeePercent,
                    TakerFeePercent = fees.TakerFeePercent,
                    FeeScheduleType = fees.FeeScheduleType,
                    Notes = fees.Notes
                };

                return new RequestResponse(response, 200, "Fees updated");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to update fees", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Tests connectivity to the account's exchange API by making
        /// an HTTP request to the base URL. Returns timing and status.
        /// </summary>
        public async Task<RequestResponse> TestAccountConnection(int accountId, int appUserId)
        {
            try
            {
                var account = await _db.Accounts
                    .Include(a => a.Connection)
                    .FirstOrDefaultAsync(a => a.Id == accountId && a.AppUserId == appUserId);

                if (account == null)
                    return new RequestResponse(null, 404, "Account not found");

                var baseUrl = account.Connection?.DefaultBaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return new RequestResponse(null, 400, "No base URL configured for this account's connection");

                // Try known health endpoints based on service type
                var testUrl = GetHealthCheckUrl(baseUrl, account.Connection?.IconKey);

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var stopwatch = Stopwatch.StartNew();
                TestConnectionResponse result;

                try
                {
                    var response = await client.GetAsync(testUrl);
                    stopwatch.Stop();

                    result = new TestConnectionResponse
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Message = response.IsSuccessStatusCode
                            ? "Connection successful"
                            : $"HTTP {(int)response.StatusCode} — {response.ReasonPhrase}",
                        TestedAt = DateTime.UtcNow
                    };
                }
                catch (TaskCanceledException)
                {
                    stopwatch.Stop();
                    result = new TestConnectionResponse
                    {
                        Success = false,
                        StatusCode = 0,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Message = "Connection timed out after 10 seconds",
                        TestedAt = DateTime.UtcNow
                    };
                }
                catch (HttpRequestException ex)
                {
                    stopwatch.Stop();
                    result = new TestConnectionResponse
                    {
                        Success = false,
                        StatusCode = 0,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        Message = $"Connection failed — {ex.Message}",
                        TestedAt = DateTime.UtcNow
                    };
                }

                return new RequestResponse(result, 200, "Test complete");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to test connection", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Returns the best health-check URL for a given service.
        /// Falls back to base URL if the service isn't recognized.
        /// </summary>
        private string GetHealthCheckUrl(string baseUrl, string iconKey)
        {
            baseUrl = baseUrl.TrimEnd('/');

            return iconKey?.ToLower() switch
            {
                "binance" => $"{baseUrl}/api/v3/ping",
                "coinbase" => $"{baseUrl}/v2/time",
                "kraken" => $"{baseUrl}/0/public/Time",
                "alpaca" => $"{baseUrl}/v2/clock",
                _ => baseUrl
            };
        }

        // =============================================
        // CREDENTIAL SUMMARY BUILDER
        // =============================================

        private CredentialSummary BuildCredentialSummary(UserCredential credential)
        {
            if (credential == null)
            {
                return new CredentialSummary { HasCredentials = false };
            }

            return new CredentialSummary
            {
                HasCredentials = true,
                Label = credential.Label ?? "",
                MaskedApiKey = MaskValue(credential.EncryptedApiKey),
                MaskedApiSecret = MaskValue(credential.EncryptedApiSecret),
                HasPassphrase = !string.IsNullOrEmpty(credential.EncryptedPassphrase),
                HasUsername = !string.IsNullOrEmpty(credential.EncryptedUsername),
                Permissions = credential.Permissions ?? "",
                LastUsedAt = credential.LastUsedAt,
                ExpiresAt = credential.ExpiresAt
            };
        }

        // =============================================
        // NEW FLOW — SERVICE CATALOG ACCOUNT CREATION
        // =============================================

        /// <summary>
        /// Creates an Account linked to a Connection (service) and stores
        /// encrypted credentials in UserCredential using the user's DEK.
        /// </summary>
        public async Task<RequestResponse> CreateAccountFromRequest(CreateAccountRequest request, int appUserId)
        {
            try
            {
                // Load the user to get their encrypted DEK
                var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == appUserId && u.IsActive);
                if (appUser == null)
                    return new RequestResponse(null, 404, "User not found");

                // Create the Account entity
                var account = new Account
                {
                    Name = request.Name,
                    Description = request.Description ?? "",
                    ConnectionId = request.ConnectionId,
                    AccountTypeId = request.AccountTypeId,
                    AppUserId = appUserId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.Accounts.AddAsync(account);
                await _db.SaveChangesAsync();

                // Create UserCredential if credentials were provided
                UserCredential credential = null;
                if (request.Credentials != null && HasAnyCredential(request.Credentials))
                {
                    var userKey = appUser.EncryptedDataKey;
                    if (string.IsNullOrEmpty(userKey))
                        return new RequestResponse(null, 400, "User encryption key not configured — cannot store credentials securely");

                    credential = new UserCredential
                    {
                        AppUserId = appUserId,
                        ConnectionId = request.ConnectionId ?? 0,
                        Label = request.Credentials.Label ?? request.Name,
                        Permissions = request.Credentials.Permissions,
                        KeyVersion = 1,
                        IsActive = true,
                        Name = $"{request.Name} credentials",
                        CreatedAt = DateTime.UtcNow,

                        // Encrypt each field with the user's DEK
                        EncryptedApiKey = EncryptIfPresent(request.Credentials.ApiKey, userKey),
                        EncryptedApiSecret = EncryptIfPresent(request.Credentials.ApiSecret, userKey),
                        EncryptedPassphrase = EncryptIfPresent(request.Credentials.Passphrase, userKey),
                        EncryptedUsername = EncryptIfPresent(request.Credentials.Username, userKey),
                        EncryptedPassword = EncryptIfPresent(request.Credentials.Password, userKey)
                    };

                    await _db.UserCredentials.AddAsync(credential);
                    await _db.SaveChangesAsync();
                }

                // Return the AccountResponse (never plaintext credentials)
                var response = BuildAccountResponse(account, credential);
                return new RequestResponse(response, 200, "Account created");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "Failed to create account", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Returns all accounts for a user with connection metadata and masked credential info.
        /// </summary>
        public async Task<RequestResponse> GetAccountsForUser(int appUserId)
        {
            try
            {
                var accounts = await _db.Accounts
                    .Include(a => a.Connection)
                    .Include(a => a.AccountType)
                    .Where(a => a.AppUserId == appUserId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                // Load credentials for these accounts to show masked summaries
                var connectionIds = accounts
                    .Where(a => a.ConnectionId.HasValue)
                    .Select(a => a.ConnectionId.Value)
                    .ToList();

                var credentials = await _db.UserCredentials
                    .Where(uc => uc.AppUserId == appUserId && connectionIds.Contains(uc.ConnectionId))
                    .ToListAsync();

                var credLookup = credentials.ToDictionary(c => c.ConnectionId);

                var responses = accounts.Select(a =>
                {
                    credLookup.TryGetValue(a.ConnectionId ?? 0, out var cred);
                    return BuildAccountResponse(a, cred);
                }).ToList();

                return new RequestResponse(responses, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        // =============================================
        // RESPONSE BUILDING
        // =============================================

        private AccountResponse BuildAccountResponse(Account account, UserCredential credential)
        {
            return new AccountResponse
            {
                Id = account.Id,
                Name = account.Name,
                Description = account.Description,
                IsActive = account.IsActive,
                CreatedAt = account.CreatedAt,
                LastModifiedAt = account.LastModifiedAt,

                // Connection / service info
                ConnectionId = account.ConnectionId,
                ConnectionName = account.Connection?.Name ?? "",
                ConnectionIconKey = account.Connection?.IconKey ?? "",
                ConnectionColor = account.Connection?.Color ?? "",
                ConnectionCategory = account.Connection?.Category ?? "",

                // Account type
                AccountTypeId = account.AccountTypeId,
                AccountTypeName = account.AccountType?.Name ?? "",

                // Credential summary (never plaintext)
                HasCredentials = credential != null,
                CredentialLabel = credential?.Label ?? "",
                MaskedApiKey = MaskValue(credential?.EncryptedApiKey),
                CredentialLastUsedAt = credential?.LastUsedAt,

                // Funding
                IsPrimaryFunding = account.IsPrimaryFunding
            };
        }

        /// <summary>
        /// Returns a masked display hint from an encrypted value.
        /// Shows "••••XXXX" where XXXX is a hash-based suffix (NOT the actual key).
        /// </summary>
        private string MaskValue(string encryptedValue)
        {
            if (string.IsNullOrEmpty(encryptedValue)) return null;
            // Use last 4 chars of the encrypted ciphertext as a visual identifier
            // This is safe — it's from the ciphertext, not the plaintext
            var suffix = encryptedValue.Length >= 4
                ? encryptedValue.Substring(encryptedValue.Length - 4)
                : "****";
            return $"••••{suffix}";
        }

        // =============================================
        // ENCRYPTION HELPERS
        // =============================================

        private string EncryptIfPresent(string plaintext, string encryptedUserKey)
        {
            if (string.IsNullOrWhiteSpace(plaintext)) return null;
            return _encryption.Encrypt(plaintext, encryptedUserKey);
        }

        private bool HasAnyCredential(AccountCredentials creds)
        {
            return !string.IsNullOrWhiteSpace(creds.ApiKey)
                || !string.IsNullOrWhiteSpace(creds.ApiSecret)
                || !string.IsNullOrWhiteSpace(creds.Passphrase)
                || !string.IsNullOrWhiteSpace(creds.Username)
                || !string.IsNullOrWhiteSpace(creds.Password);
        }

        // =============================================
        // EXISTING CRUD (retained for backward compatibility)
        // =============================================

        public async Task<RequestResponse> GetAccounts()
        {
            try
            {
                List<Account> accounts = await _db.Accounts
                .ToListAsync();

                return new RequestResponse(accounts, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetAccount(int id)
        {
            try
            {
                var account = await _db.Accounts
                    .Include(a => a.Connection)
                    .Include(a => a.AccountType)
                    .FirstOrDefaultAsync(a => a.Id == id);

                return new RequestResponse(account, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetAccountTypes()
        {
            try
            {
                var accountTypes = await _db.AccountTypes
                    .ToListAsync();

                return new RequestResponse(accountTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteAccount(int id)
        {
            try
            {
                _db.Remove(_db.Accounts
                    .Where(w => w.Id == id)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteAccounts(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.Accounts
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveAccount(Account account)
        {
            try
            {
                if (account.Id == 0 || account.IsNewAccount)
                    return await CreateAccount(account);
                else
                    return await UpdateAccountLegacy(account);
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> CreateAccount(Account account)
        {
            Account newAccount = new Account
            {
                Name = account.Name,
                Description = account.Description,
                Notes = account.Notes,
                IsActive = true,
                CreatedAt = DateTime.Now.ToUniversalTime(),
                AccountDetails = account.AccountDetails
            };

            try
            {
                await _db.AddAsync(newAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(newAccount, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateAccountLegacy(Account account)
        {
            try
            {
                Account existingAccount = await _db.Accounts
                    .Where(w => w.Id == account.Id)
                    .FirstOrDefaultAsync();

                existingAccount.Name = account.Name;
                existingAccount.Description = account.Description;
                existingAccount.Notes = account.Notes;
                existingAccount.IsActive = account.IsActive;
                existingAccount.LastModifiedAt = DateTime.Now.ToUniversalTime();

                _db.Update(existingAccount);
                await _db.SaveChangesAsync();

                return new RequestResponse(account, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        // DETAILS
        public async Task<RequestResponse> GetAccountDetails(int id)
        {
            try
            {
                var accountDetails = await _db.AccountDetails
                    .Include("AccountType")
                    .Where(w => w.AccountId == id)
                    .FirstOrDefaultAsync();

                return new RequestResponse(accountDetails, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveAccountDetails(AccountDetails accountDetails)
        {
            try
            {
                var account = await _db.Accounts
                    .FirstOrDefaultAsync(x => x.Id == accountDetails.AccountId);

                if (account != null)
                {
                    // add details to existing account
                }
                else
                {
                    // create new account and apply details
                    Account newAccount = new Account
                    {
                        AccountDetails = accountDetails,
                        CreatedAt = DateTime.Now.ToUniversalTime(),
                        Description = "New Account",
                        IsActive = true,
                        Name = accountDetails.Name
                    };

                    await CreateAccount(newAccount);
                }

                await _db.SaveChangesAsync();
                return new RequestResponse(accountDetails, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
    }
}
