#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Services.Security;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services
{
    /// <summary>
    /// Orchestrates Plaid operations: resolves per-user credentials,
    /// combines PlaidApiClient (HTTP calls) with database persistence
    /// and credential encryption. Called by PlaidController.
    /// </summary>
    public class PlaidService
    {
        private readonly EgibiDbContext _db;
        private readonly PlaidApiClient _plaidApi;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<PlaidService> _logger;

        public PlaidService(
            EgibiDbContext db,
            PlaidApiClient plaidApi,
            IEncryptionService encryption,
            ILogger<PlaidService> logger)
        {
            _db = db;
            _plaidApi = plaidApi;
            _encryption = encryption;
            _logger = logger;
        }

        // =============================================
        // USER PLAID CONFIG MANAGEMENT
        // =============================================

        /// <summary>
        /// Returns whether the user has Plaid credentials configured.
        /// </summary>
        public async Task<RequestResponse> GetPlaidConfigStatus(int userId)
        {
            var config = await _db.UserPlaidConfigs
                .FirstOrDefaultAsync(c => c.AppUserId == userId && c.IsActive);

            return new RequestResponse(
                new
                {
                    isConfigured = config != null,
                    environment = config?.Environment ?? "sandbox",
                    createdAt = config?.CreatedAt,
                    lastModifiedAt = config?.LastModifiedAt
                },
                200, "OK");
        }

        /// <summary>
        /// Saves (creates or updates) the user's Plaid developer credentials.
        /// </summary>
        public async Task<RequestResponse> SavePlaidConfig(PlaidConfigRequest request, int userId)
        {
            try
            {
                var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (appUser == null)
                    return new RequestResponse(null, 404, "User not found");

                if (string.IsNullOrEmpty(appUser.EncryptedDataKey))
                    return new RequestResponse(null, 400, "User encryption key not configured");

                // Validate inputs
                if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.Secret))
                    return new RequestResponse(null, 400, "Client ID and Secret are required");

                var validEnvs = new[] { "sandbox", "development", "production" };
                var env = (request.Environment ?? "sandbox").ToLower();
                if (!validEnvs.Contains(env))
                    return new RequestResponse(null, 400, "Environment must be sandbox, development, or production");

                // Encrypt credentials
                var encryptedClientId = _encryption.Encrypt(request.ClientId.Trim(), appUser.EncryptedDataKey);
                var encryptedSecret = _encryption.Encrypt(request.Secret.Trim(), appUser.EncryptedDataKey);

                // Upsert
                var existing = await _db.UserPlaidConfigs
                    .FirstOrDefaultAsync(c => c.AppUserId == userId);

                if (existing != null)
                {
                    existing.EncryptedClientId = encryptedClientId;
                    existing.EncryptedSecret = encryptedSecret;
                    existing.Environment = env;
                    existing.IsActive = true;
                    existing.LastModifiedAt = DateTime.UtcNow;
                    _db.Update(existing);
                }
                else
                {
                    var config = new UserPlaidConfig
                    {
                        AppUserId = userId,
                        EncryptedClientId = encryptedClientId,
                        EncryptedSecret = encryptedSecret,
                        Environment = env,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _db.AddAsync(config);
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation("Plaid config saved for user {UserId}, environment: {Env}", userId, env);
                return new RequestResponse(new { environment = env }, 200, "Plaid configuration saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavePlaidConfig failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to save Plaid configuration", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Deletes the user's Plaid configuration.
        /// </summary>
        public async Task<RequestResponse> DeletePlaidConfig(int userId)
        {
            try
            {
                var config = await _db.UserPlaidConfigs
                    .FirstOrDefaultAsync(c => c.AppUserId == userId);

                if (config == null)
                    return new RequestResponse(null, 404, "No Plaid configuration found");

                _db.Remove(config);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Plaid config deleted for user {UserId}", userId);
                return new RequestResponse(null, 200, "Plaid configuration deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePlaidConfig failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to delete Plaid configuration", new ResponseError(ex));
            }
        }

        // =============================================
        // CREDENTIAL RESOLUTION
        // =============================================

        /// <summary>
        /// Decrypts the user's stored Plaid credentials.
        /// Returns (credentials, null) on success, or (null, errorResponse) on failure.
        /// </summary>
        private async Task<(PlaidUserCredentials creds, RequestResponse error)> GetUserPlaidCredentials(int userId)
        {
            var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (appUser == null)
                return (null, new RequestResponse(null, 404, "User not found"));

            if (string.IsNullOrEmpty(appUser.EncryptedDataKey))
                return (null, new RequestResponse(null, 400, "User encryption key not configured"));

            var config = await _db.UserPlaidConfigs
                .FirstOrDefaultAsync(c => c.AppUserId == userId && c.IsActive);

            if (config == null)
                return (null, new RequestResponse(null, 400,
                    "Plaid is not configured. Please set up your Plaid developer credentials in the Funding section before connecting a bank account."));

            try
            {
                var clientId = _encryption.Decrypt(config.EncryptedClientId, appUser.EncryptedDataKey);
                var secret = _encryption.Decrypt(config.EncryptedSecret, appUser.EncryptedDataKey);

                return (new PlaidUserCredentials
                {
                    ClientId = clientId,
                    Secret = secret,
                    Environment = config.Environment
                }, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt Plaid credentials for user {UserId}", userId);
                return (null, new RequestResponse(null, 500, "Failed to decrypt Plaid credentials"));
            }
        }

        // =============================================
        // LINK TOKEN
        // =============================================

        public async Task<RequestResponse> CreateLinkToken(int userId)
        {
            try
            {
                var (creds, error) = await GetUserPlaidCredentials(userId);
                if (error != null) return error;

                var response = await _plaidApi.CreateLinkToken(creds, userId.ToString());

                if (!response.IsSuccess)
                    return new RequestResponse(null, 400, response.ErrorMessage ?? "Failed to create link token");

                return new RequestResponse(
                    new PlaidLinkTokenResponse
                    {
                        LinkToken = response.Data.LinkToken,
                        Expiration = response.Data.Expiration
                    },
                    200, "Link token created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateLinkToken failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to create link token", new ResponseError(ex));
            }
        }

        // =============================================
        // EXCHANGE PUBLIC TOKEN
        // =============================================

        public async Task<RequestResponse> ExchangePublicToken(PlaidExchangeTokenRequest request, int userId)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                // 1. Exchange the public token via Plaid API
                var exchangeResult = await _plaidApi.ExchangePublicToken(creds, request.PublicToken);
                if (!exchangeResult.IsSuccess)
                    return new RequestResponse(null, 400, exchangeResult.ErrorMessage ?? "Token exchange failed");

                var accessToken = exchangeResult.Data.AccessToken;
                var itemId = exchangeResult.Data.ItemId;

                // 2. Get user's encryption key
                var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
                if (appUser == null)
                    return new RequestResponse(null, 404, "User not found");

                // 3. Find the Plaid connection
                var plaidConnection = await _db.Connections
                    .FirstOrDefaultAsync(c => c.LinkMethod == "plaid_link" && c.Category == "funding_provider" && c.IsActive);

                if (plaidConnection == null)
                    return new RequestResponse(null, 500, "Plaid funding provider not configured");

                // 4. Encrypt the access token
                var encryptedAccessToken = _encryption.Encrypt(accessToken, appUser.EncryptedDataKey);

                // 5. Find the selected account metadata
                var selectedAccount = request.Accounts?.FirstOrDefault(a => a.Id == request.SelectedAccountId);

                // 6. Demote any existing primary funding source
                var existingPrimary = await _db.FundingSources
                    .FirstOrDefaultAsync(f => f.AppUserId == userId && f.IsPrimary);

                if (existingPrimary != null)
                {
                    existingPrimary.IsPrimary = false;
                    existingPrimary.LastModifiedAt = DateTime.UtcNow;
                }

                // 7. Create the funding source
                var fundingSource = new FundingSource
                {
                    Name = request.AccountName ?? selectedAccount?.Name ?? "Bank Account",
                    Description = request.Institution?.Name ?? "Plaid-linked bank account",
                    AppUserId = userId,
                    ConnectionId = plaidConnection.Id,
                    IsPrimary = true,
                    LinkMethod = "plaid_link",
                    PlaidItemId = itemId,
                    EncryptedPlaidAccessToken = encryptedAccessToken,
                    PlaidAccountId = request.SelectedAccountId,
                    PlaidAccountName = selectedAccount?.Name,
                    PlaidAccountMask = selectedAccount?.Mask,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _db.FundingSources.AddAsync(fundingSource);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Plaid funding source created for user {UserId}, item {ItemId}", userId, itemId);

                return new RequestResponse(
                    new { fundingSourceId = fundingSource.Id, institutionName = request.Institution?.Name },
                    200, "Funding source linked via Plaid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExchangePublicToken failed for user {UserId}", userId);
                return new RequestResponse(null, 500, "Failed to exchange token", new ResponseError(ex));
            }
        }

        // =============================================
        // GET PLAID FUNDING DETAILS
        // =============================================

        public async Task<PlaidFundingDetails> GetPlaidFundingDetails(int fundingSourceId, int userId)
        {
            var fs = await _db.FundingSources
                .Include(f => f.Connection)
                .FirstOrDefaultAsync(f => f.Id == fundingSourceId && f.AppUserId == userId && f.LinkMethod == "plaid_link");

            if (fs == null) return null;

            return new PlaidFundingDetails
            {
                PlaidItemId = int.TryParse(fs.PlaidItemId, out var pid) ? pid : 0,
                InstitutionId = null,
                InstitutionName = fs.Description,
                PlaidAccountId = fs.PlaidAccountId,
                AccountName = fs.PlaidAccountName,
                Mask = fs.PlaidAccountMask,
                LastSyncedAt = fs.LastModifiedAt
            };
        }

        // =============================================
        // REFRESH BALANCES
        // =============================================

        public async Task<RequestResponse> RefreshBalances(int fundingSourceId, int userId)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                var (accessToken, error) = await GetDecryptedAccessToken(fundingSourceId, userId);
                if (error != null) return error;

                var result = await _plaidApi.GetBalances(creds, accessToken);
                if (!result.IsSuccess)
                    return new RequestResponse(null, 400, result.ErrorMessage ?? "Failed to fetch balances");

                var balances = result.Data.Accounts?.Select(a => new PlaidBalanceResponse
                {
                    PlaidAccountId = a.AccountId,
                    AccountName = a.Name,
                    Mask = a.Mask,
                    AccountType = a.Type,
                    AccountSubtype = a.Subtype,
                    AvailableBalance = a.Balances?.Available,
                    CurrentBalance = a.Balances?.Current,
                    IsoCurrencyCode = a.Balances?.IsoCurrencyCode
                }).ToArray();

                var fs = await _db.FundingSources.FindAsync(fundingSourceId);
                if (fs != null)
                {
                    fs.LastModifiedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                return new RequestResponse(balances, 200, "Balances refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshBalances failed for funding source {FundingSourceId}", fundingSourceId);
                return new RequestResponse(null, 500, "Failed to refresh balances", new ResponseError(ex));
            }
        }

        // =============================================
        // GET TRANSACTIONS
        // =============================================

        public async Task<RequestResponse> GetTransactions(int fundingSourceId, int userId, int days = 30)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                var (accessToken, error) = await GetDecryptedAccessToken(fundingSourceId, userId);
                if (error != null) return error;

                var endDate = DateTime.UtcNow;
                var startDate = endDate.AddDays(-days);

                var result = await _plaidApi.GetTransactions(creds, accessToken, startDate, endDate);
                if (!result.IsSuccess)
                    return new RequestResponse(null, 400, result.ErrorMessage ?? "Failed to fetch transactions");

                var transactions = result.Data.Transactions?.Select(t => new PlaidTransactionResponse
                {
                    TransactionId = t.TransactionId,
                    AccountId = t.AccountId,
                    Amount = t.Amount,
                    IsoCurrencyCode = t.IsoCurrencyCode,
                    Date = t.Date,
                    Name = t.Name,
                    MerchantName = t.MerchantName,
                    Category = t.Category,
                    Pending = t.Pending
                }).ToArray();

                return new RequestResponse(
                    new { transactions, totalTransactions = result.Data.TotalTransactions },
                    200, "Transactions retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetTransactions failed for funding source {FundingSourceId}", fundingSourceId);
                return new RequestResponse(null, 500, "Failed to get transactions", new ResponseError(ex));
            }
        }

        // =============================================
        // GET AUTH (ACH NUMBERS)
        // =============================================

        public async Task<RequestResponse> GetAuth(int fundingSourceId, int userId)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                var (accessToken, error) = await GetDecryptedAccessToken(fundingSourceId, userId);
                if (error != null) return error;

                var result = await _plaidApi.GetAuth(creds, accessToken);
                if (!result.IsSuccess)
                    return new RequestResponse(null, 400, result.ErrorMessage ?? "Failed to fetch auth data");

                var authData = result.Data.Numbers?.Ach?.Select(a => new PlaidAuthResponse
                {
                    AccountId = a.AccountId,
                    AccountNumber = a.Account,
                    RoutingNumber = a.Routing,
                    WireRoutingNumber = a.WireRouting
                }).ToArray();

                return new RequestResponse(authData, 200, "Auth data retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAuth failed for funding source {FundingSourceId}", fundingSourceId);
                return new RequestResponse(null, 500, "Failed to get auth data", new ResponseError(ex));
            }
        }

        // =============================================
        // GET IDENTITY
        // =============================================

        public async Task<RequestResponse> GetIdentity(int fundingSourceId, int userId)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                var (accessToken, error) = await GetDecryptedAccessToken(fundingSourceId, userId);
                if (error != null) return error;

                var result = await _plaidApi.GetIdentity(creds, accessToken);
                if (!result.IsSuccess)
                    return new RequestResponse(null, 400, result.ErrorMessage ?? "Failed to fetch identity data");

                var identityData = result.Data.Accounts?.Select(a => new PlaidIdentityResponse
                {
                    AccountId = a.AccountId,
                    Owners = a.Owners?.Select(o => new PlaidOwnerInfo
                    {
                        Names = o.Names,
                        Emails = o.Emails?.Select(e => e.Data).ToArray(),
                        PhoneNumbers = o.PhoneNumbers?.Select(p => p.Data).ToArray()
                    }).ToArray()
                }).ToArray();

                return new RequestResponse(identityData, 200, "Identity data retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetIdentity failed for funding source {FundingSourceId}", fundingSourceId);
                return new RequestResponse(null, 500, "Failed to get identity data", new ResponseError(ex));
            }
        }

        // =============================================
        // REMOVE ITEM
        // =============================================

        public async Task<RequestResponse> RemoveItem(int fundingSourceId, int userId)
        {
            try
            {
                var (creds, credError) = await GetUserPlaidCredentials(userId);
                if (credError != null) return credError;

                var (accessToken, error) = await GetDecryptedAccessToken(fundingSourceId, userId);
                if (error != null) return error;

                var result = await _plaidApi.RemoveItem(creds, accessToken);
                if (!result.IsSuccess)
                    _logger.LogWarning("Plaid item removal returned error (proceeding anyway): {Error}", result.ErrorMessage);

                var fs = await _db.FundingSources.FindAsync(fundingSourceId);
                if (fs != null)
                {
                    fs.IsActive = false;
                    fs.IsPrimary = false;
                    fs.EncryptedPlaidAccessToken = null;
                    fs.LastModifiedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                _logger.LogInformation("Plaid item removed for user {UserId}, funding source {FundingSourceId}", userId, fundingSourceId);
                return new RequestResponse(fundingSourceId, 200, "Plaid item removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveItem failed for funding source {FundingSourceId}", fundingSourceId);
                return new RequestResponse(null, 500, "Failed to remove Plaid item", new ResponseError(ex));
            }
        }

        // =============================================
        // PRIVATE HELPERS
        // =============================================

        private async Task<(string accessToken, RequestResponse error)> GetDecryptedAccessToken(int fundingSourceId, int userId)
        {
            var fs = await _db.FundingSources
                .FirstOrDefaultAsync(f => f.Id == fundingSourceId && f.AppUserId == userId);

            if (fs == null)
                return (null, new RequestResponse(null, 404, "Funding source not found"));

            if (fs.LinkMethod != "plaid_link")
                return (null, new RequestResponse(null, 400, "Funding source is not a Plaid-linked account"));

            if (string.IsNullOrEmpty(fs.EncryptedPlaidAccessToken))
                return (null, new RequestResponse(null, 400, "Plaid access token not available"));

            var appUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId);
            if (appUser == null || string.IsNullOrEmpty(appUser.EncryptedDataKey))
                return (null, new RequestResponse(null, 400, "User encryption key not configured"));

            var accessToken = _encryption.Decrypt(fs.EncryptedPlaidAccessToken, appUser.EncryptedDataKey);
            return (accessToken, null);
        }
    }
}
