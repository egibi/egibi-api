#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Models;
using egibi_api.Services.Security;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace egibi_api.Services
{
    public class PlaidService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<PlaidService> _logger;

        // Plaid config
        private readonly string _clientId;
        private readonly string _secret;
        private readonly string _environment;
        private readonly string _baseUrl;

        private static readonly string[] PlaidProducts = { "auth", "transactions", "balance", "identity" };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public PlaidService(
            EgibiDbContext db,
            IEncryptionService encryption,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<PlaidService> logger)
        {
            _db = db;
            _encryption = encryption;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;

            _clientId = _config["Plaid:ClientId"] ?? "";
            _secret = _config["Plaid:Secret"] ?? "";
            _environment = _config["Plaid:Environment"] ?? "development";
            _baseUrl = _environment switch
            {
                "sandbox" => "https://sandbox.plaid.com",
                "production" => "https://production.plaid.com",
                _ => "https://development.plaid.com"
            };
        }

        // =============================================
        // CREATE LINK TOKEN
        // =============================================

        /// <summary>
        /// Creates a Plaid link_token for initializing Plaid Link on the frontend.
        /// </summary>
        public async Task<RequestResponse> CreateLinkToken(int appUserId)
        {
            var user = await _db.AppUsers.FindAsync(appUserId);
            if (user == null)
                return new RequestResponse(null, 401, "User not found");

            var requestBody = new
            {
                client_id = _clientId,
                secret = _secret,
                user = new { client_user_id = appUserId.ToString() },
                client_name = "Egibi",
                products = PlaidProducts,
                country_codes = new[] { "US" },
                language = "en"
            };

            var result = await PostPlaid<PlaidLinkTokenApiResponse>("/link/token/create", requestBody);

            if (result == null)
                return new RequestResponse(null, 500, "Failed to create Plaid link token");

            var response = new PlaidLinkTokenResponse
            {
                LinkToken = result.LinkToken,
                Expiration = result.Expiration
            };

            return new RequestResponse(response, 200, "OK");
        }

        // =============================================
        // EXCHANGE PUBLIC TOKEN
        // =============================================

        /// <summary>
        /// Exchanges a Plaid public_token for a permanent access_token,
        /// creates the Egibi Account + PlaidItem + PlaidAccounts,
        /// and marks the selected account as the primary funding source.
        /// </summary>
        public async Task<RequestResponse> ExchangePublicToken(PlaidExchangeTokenRequest request, int appUserId)
        {
            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null)
                return new RequestResponse(null, 401, "User not found");

            // 1. Exchange public_token for access_token
            var exchangeBody = new
            {
                client_id = _clientId,
                secret = _secret,
                public_token = request.PublicToken
            };

            var exchangeResult = await PostPlaid<PlaidTokenExchangeApiResponse>("/item/public_token/exchange", exchangeBody);

            if (exchangeResult == null || string.IsNullOrEmpty(exchangeResult.AccessToken))
                return new RequestResponse(null, 500, "Failed to exchange Plaid token");

            // Get the Plaid connection from seed data
            var plaidConnection = await _db.Connections
                .FirstOrDefaultAsync(c => c.IconKey == "plaid" && c.Category == "funding_provider");

            if (plaidConnection == null)
                return new RequestResponse(null, 500, "Plaid connection not found in service catalog");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // 2. Demote any existing primary funding source
                var existingPrimary = await _db.Accounts
                    .Where(a => a.AppUserId == appUserId && a.IsPrimaryFunding)
                    .ToListAsync();

                foreach (var existing in existingPrimary)
                {
                    existing.IsPrimaryFunding = false;
                    existing.LastModifiedAt = DateTime.UtcNow;
                }

                // 3. Create the Egibi Account
                var institutionName = request.Institution?.Name ?? "Linked Bank";
                var accountName = !string.IsNullOrWhiteSpace(request.AccountName)
                    ? request.AccountName
                    : $"{institutionName} Funding Account";

                var account = new Account
                {
                    Name = accountName,
                    Description = $"Bank account linked via Plaid ({institutionName})",
                    ConnectionId = plaidConnection.Id,
                    AppUserId = appUserId,
                    IsPrimaryFunding = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.Accounts.Add(account);
                await _db.SaveChangesAsync();

                // 4. Create PlaidItem with encrypted access_token
                var plaidItem = new PlaidItem
                {
                    Name = institutionName,
                    Description = $"Plaid-linked bank: {institutionName}",
                    AppUserId = appUserId,
                    AccountId = account.Id,
                    PlaidItemId = exchangeResult.ItemId,
                    EncryptedAccessToken = _encryption.Encrypt(exchangeResult.AccessToken, appUser.EncryptedDataKey),
                    InstitutionId = request.Institution?.InstitutionId,
                    InstitutionName = institutionName,
                    EnabledProducts = string.Join(",", PlaidProducts),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                _db.PlaidItems.Add(plaidItem);
                await _db.SaveChangesAsync();

                // 5. Store all accounts from metadata
                if (request.Accounts != null)
                {
                    foreach (var acctMeta in request.Accounts)
                    {
                        var plaidAccount = new PlaidAccount
                        {
                            Name = acctMeta.Name ?? "Account",
                            PlaidItemId = plaidItem.Id,
                            PlaidAccountId = acctMeta.Id,
                            OfficialName = acctMeta.Name,
                            Mask = acctMeta.Mask,
                            AccountType = acctMeta.Type,
                            AccountSubtype = acctMeta.Subtype,
                            IsSelectedFunding = acctMeta.Id == request.SelectedAccountId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                        };

                        _db.PlaidAccounts.Add(plaidAccount);
                    }

                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // 6. Fetch initial balances
                _ = Task.Run(async () =>
                {
                    try { await RefreshBalancesInternal(plaidItem.Id, appUserId); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to fetch initial Plaid balances"); }
                });

                return new RequestResponse(new { accountId = account.Id, plaidItemId = plaidItem.Id }, 200, "Bank linked successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to exchange Plaid token for user {UserId}", appUserId);
                return new RequestResponse(null, 500, "Failed to link bank account", new ResponseError(ex));
            }
        }

        // =============================================
        // GET PLAID FUNDING DETAILS
        // =============================================

        /// <summary>
        /// Returns detailed Plaid funding info for the user's primary funding source.
        /// </summary>
        public async Task<PlaidFundingDetails> GetPlaidFundingDetails(int accountId, int appUserId)
        {
            var plaidItem = await _db.PlaidItems
                .Include(pi => pi.PlaidAccounts)
                .FirstOrDefaultAsync(pi => pi.AccountId == accountId && pi.AppUserId == appUserId);

            if (plaidItem == null) return null;

            var selectedAccount = plaidItem.PlaidAccounts?.FirstOrDefault(a => a.IsSelectedFunding)
                ?? plaidItem.PlaidAccounts?.FirstOrDefault();

            return new PlaidFundingDetails
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

        // =============================================
        // REFRESH BALANCES
        // =============================================

        /// <summary>
        /// Refreshes balance data for all accounts in a Plaid item.
        /// </summary>
        public async Task<RequestResponse> RefreshBalances(int plaidItemId, int appUserId)
        {
            var balances = await RefreshBalancesInternal(plaidItemId, appUserId);
            if (balances == null)
                return new RequestResponse(null, 500, "Failed to refresh balances");

            return new RequestResponse(balances, 200, "OK");
        }

        private async Task<List<PlaidBalanceResponse>> RefreshBalancesInternal(int plaidItemId, int appUserId)
        {
            var plaidItem = await _db.PlaidItems
                .Include(pi => pi.PlaidAccounts)
                .FirstOrDefaultAsync(pi => pi.Id == plaidItemId && pi.AppUserId == appUserId);

            if (plaidItem == null) return null;

            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null) return null;

            var accessToken = _encryption.Decrypt(plaidItem.EncryptedAccessToken, appUser.EncryptedDataKey);

            var requestBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken
            };

            var result = await PostPlaid<PlaidBalanceApiResponse>("/accounts/balance/get", requestBody);
            if (result?.Accounts == null) return null;

            var responses = new List<PlaidBalanceResponse>();

            foreach (var apiAcct in result.Accounts)
            {
                // Update cached balances in DB
                var dbAccount = plaidItem.PlaidAccounts?.FirstOrDefault(a => a.PlaidAccountId == apiAcct.AccountId);
                if (dbAccount != null)
                {
                    dbAccount.AvailableBalance = apiAcct.Balances?.Available;
                    dbAccount.CurrentBalance = apiAcct.Balances?.Current;
                    dbAccount.IsoCurrencyCode = apiAcct.Balances?.IsoCurrencyCode ?? "USD";
                    dbAccount.BalanceLastUpdatedAt = DateTime.UtcNow;
                }

                responses.Add(new PlaidBalanceResponse
                {
                    PlaidAccountId = apiAcct.AccountId,
                    AccountName = apiAcct.OfficialName ?? apiAcct.Name,
                    Mask = apiAcct.Mask,
                    AccountType = apiAcct.Type,
                    AccountSubtype = apiAcct.Subtype,
                    AvailableBalance = apiAcct.Balances?.Available,
                    CurrentBalance = apiAcct.Balances?.Current,
                    IsoCurrencyCode = apiAcct.Balances?.IsoCurrencyCode ?? "USD",
                });
            }

            plaidItem.LastSyncedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return responses;
        }

        // =============================================
        // GET TRANSACTIONS
        // =============================================

        /// <summary>
        /// Fetches recent transactions for a Plaid item.
        /// </summary>
        public async Task<RequestResponse> GetTransactions(int plaidItemId, int appUserId, int days = 30)
        {
            var plaidItem = await _db.PlaidItems.FirstOrDefaultAsync(pi => pi.Id == plaidItemId && pi.AppUserId == appUserId);
            if (plaidItem == null)
                return new RequestResponse(null, 404, "Plaid item not found");

            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null)
                return new RequestResponse(null, 401, "User not found");

            var accessToken = _encryption.Decrypt(plaidItem.EncryptedAccessToken, appUser.EncryptedDataKey);
            var startDate = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd");
            var endDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var requestBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken,
                start_date = startDate,
                end_date = endDate,
                options = new { count = 100, offset = 0 }
            };

            var result = await PostPlaid<PlaidTransactionsApiResponse>("/transactions/get", requestBody);
            if (result?.Transactions == null)
                return new RequestResponse(null, 500, "Failed to fetch transactions");

            var transactions = result.Transactions.Select(t => new PlaidTransactionResponse
            {
                TransactionId = t.TransactionId,
                AccountId = t.AccountId,
                Amount = t.Amount,
                IsoCurrencyCode = t.IsoCurrencyCode ?? "USD",
                Date = t.Date,
                Name = t.Name,
                MerchantName = t.MerchantName,
                Category = t.Category,
                Pending = t.Pending,
            }).ToList();

            return new RequestResponse(transactions, 200, "OK");
        }

        // =============================================
        // GET AUTH (ACH NUMBERS)
        // =============================================

        /// <summary>
        /// Retrieves ACH account/routing numbers for a Plaid item.
        /// </summary>
        public async Task<RequestResponse> GetAuth(int plaidItemId, int appUserId)
        {
            var plaidItem = await _db.PlaidItems.FirstOrDefaultAsync(pi => pi.Id == plaidItemId && pi.AppUserId == appUserId);
            if (plaidItem == null)
                return new RequestResponse(null, 404, "Plaid item not found");

            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null)
                return new RequestResponse(null, 401, "User not found");

            var accessToken = _encryption.Decrypt(plaidItem.EncryptedAccessToken, appUser.EncryptedDataKey);

            var requestBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken
            };

            var result = await PostPlaid<PlaidAuthApiResponse>("/auth/get", requestBody);
            if (result?.Numbers?.Ach == null)
                return new RequestResponse(null, 500, "Failed to fetch auth data");

            var authData = result.Numbers.Ach.Select(n => new PlaidAuthResponse
            {
                AccountId = n.AccountId,
                AccountNumber = MaskAccountNumber(n.Account),
                RoutingNumber = n.Routing,
                WireRoutingNumber = n.WireRouting,
            }).ToList();

            return new RequestResponse(authData, 200, "OK");
        }

        // =============================================
        // GET IDENTITY
        // =============================================

        /// <summary>
        /// Retrieves identity info (owner names, emails, phones) for a Plaid item.
        /// </summary>
        public async Task<RequestResponse> GetIdentity(int plaidItemId, int appUserId)
        {
            var plaidItem = await _db.PlaidItems.FirstOrDefaultAsync(pi => pi.Id == plaidItemId && pi.AppUserId == appUserId);
            if (plaidItem == null)
                return new RequestResponse(null, 404, "Plaid item not found");

            var appUser = await _db.AppUsers.FindAsync(appUserId);
            if (appUser == null)
                return new RequestResponse(null, 401, "User not found");

            var accessToken = _encryption.Decrypt(plaidItem.EncryptedAccessToken, appUser.EncryptedDataKey);

            var requestBody = new
            {
                client_id = _clientId,
                secret = _secret,
                access_token = accessToken
            };

            var result = await PostPlaid<PlaidIdentityApiResponse>("/identity/get", requestBody);
            if (result?.Accounts == null)
                return new RequestResponse(null, 500, "Failed to fetch identity data");

            var identityData = result.Accounts.Select(a => new PlaidIdentityResponse
            {
                AccountId = a.AccountId,
                Owners = a.Owners?.Select(o => new PlaidOwnerInfo
                {
                    Names = o.Names?.ToArray(),
                    Emails = o.Emails?.Select(e => e.Data).ToArray(),
                    PhoneNumbers = o.PhoneNumbers?.Select(p => p.Data).ToArray(),
                }).ToArray()
            }).ToList();

            return new RequestResponse(identityData, 200, "OK");
        }

        // =============================================
        // REMOVE ITEM
        // =============================================

        /// <summary>
        /// Revokes the Plaid access_token and removes the PlaidItem from the database.
        /// Does NOT delete the Egibi Account — just removes the Plaid link.
        /// </summary>
        public async Task<RequestResponse> RemoveItem(int plaidItemId, int appUserId)
        {
            var plaidItem = await _db.PlaidItems
                .Include(pi => pi.PlaidAccounts)
                .FirstOrDefaultAsync(pi => pi.Id == plaidItemId && pi.AppUserId == appUserId);

            if (plaidItem == null)
                return new RequestResponse(null, 404, "Plaid item not found");

            // Revoke the access token with Plaid
            try
            {
                var appUser = await _db.AppUsers.FindAsync(appUserId);
                if (appUser != null)
                {
                    var accessToken = _encryption.Decrypt(plaidItem.EncryptedAccessToken, appUser.EncryptedDataKey);
                    await PostPlaid<object>("/item/remove", new
                    {
                        client_id = _clientId,
                        secret = _secret,
                        access_token = accessToken
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke Plaid access token for item {ItemId}", plaidItemId);
            }

            // Remove from DB
            if (plaidItem.PlaidAccounts != null)
                _db.PlaidAccounts.RemoveRange(plaidItem.PlaidAccounts);

            _db.PlaidItems.Remove(plaidItem);
            await _db.SaveChangesAsync();

            return new RequestResponse(null, 200, "Plaid connection removed");
        }

        // =============================================
        // HTTP HELPERS
        // =============================================

        private async Task<T> PostPlaid<T>(string endpoint, object body) where T : class
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var json = JsonSerializer.Serialize(body, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_baseUrl}{endpoint}", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Plaid API error on {Endpoint}: {Status} — {Body}", endpoint, response.StatusCode, responseBody);
                    return null;
                }

                return JsonSerializer.Deserialize<T>(responseBody, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plaid API call failed: {Endpoint}", endpoint);
                return null;
            }
        }

        private static string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
                return "••••" + (accountNumber ?? "");
            return new string('•', accountNumber.Length - 4) + accountNumber[^4..];
        }

        // =============================================
        // PLAID API RESPONSE MODELS (internal)
        // =============================================

        private class PlaidLinkTokenApiResponse
        {
            public string LinkToken { get; set; }
            public string Expiration { get; set; }
            public string RequestId { get; set; }
        }

        private class PlaidTokenExchangeApiResponse
        {
            public string AccessToken { get; set; }
            public string ItemId { get; set; }
            public string RequestId { get; set; }
        }

        private class PlaidBalanceApiResponse
        {
            public List<PlaidApiAccount> Accounts { get; set; }
        }

        private class PlaidTransactionsApiResponse
        {
            public List<PlaidApiTransaction> Transactions { get; set; }
            public int TotalTransactions { get; set; }
        }

        private class PlaidAuthApiResponse
        {
            public PlaidAuthNumbers Numbers { get; set; }
        }

        private class PlaidAuthNumbers
        {
            public List<PlaidAchNumber> Ach { get; set; }
        }

        private class PlaidAchNumber
        {
            public string AccountId { get; set; }
            public string Account { get; set; }
            public string Routing { get; set; }
            public string WireRouting { get; set; }
        }

        private class PlaidIdentityApiResponse
        {
            public List<PlaidIdentityAccount> Accounts { get; set; }
        }

        private class PlaidIdentityAccount
        {
            public string AccountId { get; set; }
            public List<PlaidOwner> Owners { get; set; }
        }

        private class PlaidOwner
        {
            public List<string> Names { get; set; }
            public List<PlaidContactData> Emails { get; set; }
            public List<PlaidContactData> PhoneNumbers { get; set; }
        }

        private class PlaidContactData
        {
            public string Data { get; set; }
            public bool Primary { get; set; }
            public string Type { get; set; }
        }

        private class PlaidApiAccount
        {
            public string AccountId { get; set; }
            public string Name { get; set; }
            public string OfficialName { get; set; }
            public string Mask { get; set; }
            public string Type { get; set; }
            public string Subtype { get; set; }
            public PlaidApiBalances Balances { get; set; }
        }

        private class PlaidApiBalances
        {
            public decimal? Available { get; set; }
            public decimal? Current { get; set; }
            public decimal? Limit { get; set; }
            public string IsoCurrencyCode { get; set; }
        }

        private class PlaidApiTransaction
        {
            public string TransactionId { get; set; }
            public string AccountId { get; set; }
            public decimal Amount { get; set; }
            public string IsoCurrencyCode { get; set; }
            public string Date { get; set; }
            public string Name { get; set; }
            public string MerchantName { get; set; }
            public string[] Category { get; set; }
            public bool Pending { get; set; }
        }
    }
}
