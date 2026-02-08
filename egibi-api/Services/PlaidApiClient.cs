#nullable disable
using System.Text;
using System.Text.Json;
using egibi_api.Configuration;
using Microsoft.Extensions.Options;

namespace egibi_api.Services
{
    /// <summary>
    /// Low-level HTTP client for the Plaid API.
    /// Handles authentication, serialization, and error mapping.
    /// All methods return typed response objects — no business logic or DB access.
    /// </summary>
    public class PlaidApiClient
    {
        private readonly HttpClient _http;
        private readonly PlaidOptions _options;
        private readonly ILogger<PlaidApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public PlaidApiClient(HttpClient http, IOptions<PlaidOptions> options, ILogger<PlaidApiClient> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;

            _http.BaseAddress = new Uri(_options.BaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        // =============================================
        // LINK TOKEN
        // =============================================

        /// <summary>
        /// Creates a Plaid Link token for initializing the Link UI component.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidLinkTokenData>> CreateLinkToken(string userId)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                user = new { client_user_id = userId },
                client_name = "Egibi",
                products = _options.Products,
                country_codes = _options.CountryCodes,
                language = "en",
                redirect_uri = _options.RedirectUri,
                webhook = _options.WebhookUrl
            };

            return await PostAsync<PlaidLinkTokenData>("/link/token/create", payload);
        }

        /// <summary>
        /// Creates a Link token in update mode for re-authenticating an existing item.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidLinkTokenData>> CreateUpdateLinkToken(string userId, string accessToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                user = new { client_user_id = userId },
                client_name = "Egibi",
                country_codes = _options.CountryCodes,
                language = "en",
                access_token = accessToken
            };

            return await PostAsync<PlaidLinkTokenData>("/link/token/create", payload);
        }

        // =============================================
        // TOKEN EXCHANGE
        // =============================================

        /// <summary>
        /// Exchanges a public_token (from Link success) for a persistent access_token + item_id.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidExchangeData>> ExchangePublicToken(string publicToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                public_token = publicToken
            };

            return await PostAsync<PlaidExchangeData>("/item/public_token/exchange", payload);
        }

        // =============================================
        // BALANCES
        // =============================================

        /// <summary>
        /// Retrieves real-time balance data for all accounts in a Plaid item.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidAccountsData>> GetBalances(string accessToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                access_token = accessToken
            };

            return await PostAsync<PlaidAccountsData>("/accounts/balance/get", payload);
        }

        // =============================================
        // TRANSACTIONS
        // =============================================

        /// <summary>
        /// Retrieves transactions for a date range.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidTransactionsData>> GetTransactions(
            string accessToken, DateTime startDate, DateTime endDate, int count = 100, int offset = 0)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                access_token = accessToken,
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                options = new { count, offset }
            };

            return await PostAsync<PlaidTransactionsData>("/transactions/get", payload);
        }

        // =============================================
        // AUTH (ACH NUMBERS)
        // =============================================

        /// <summary>
        /// Retrieves ACH account/routing numbers for an item.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidAuthData>> GetAuth(string accessToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                access_token = accessToken
            };

            return await PostAsync<PlaidAuthData>("/auth/get", payload);
        }

        // =============================================
        // IDENTITY
        // =============================================

        /// <summary>
        /// Retrieves identity information (name, email, phone) for an item.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidIdentityData>> GetIdentity(string accessToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                access_token = accessToken
            };

            return await PostAsync<PlaidIdentityData>("/identity/get", payload);
        }

        // =============================================
        // ITEM MANAGEMENT
        // =============================================

        /// <summary>
        /// Revokes the access_token, permanently invalidating it.
        /// </summary>
        public async Task<PlaidApiResponse<PlaidRemoveData>> RemoveItem(string accessToken)
        {
            var payload = new
            {
                client_id = _options.ClientId,
                secret = _options.Secret,
                access_token = accessToken
            };

            return await PostAsync<PlaidRemoveData>("/item/remove", payload);
        }

        // =============================================
        // HTTP HELPER
        // =============================================

        private async Task<PlaidApiResponse<T>> PostAsync<T>(string endpoint, object payload) where T : class
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync(endpoint, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Plaid API error on {Endpoint}: {StatusCode} — {Body}",
                        endpoint, (int)response.StatusCode, body);

                    // Try to parse Plaid's structured error
                    try
                    {
                        var error = JsonSerializer.Deserialize<PlaidErrorBody>(body, _jsonOptions);
                        return PlaidApiResponse<T>.Fail(
                            error?.ErrorMessage ?? $"Plaid API returned {(int)response.StatusCode}",
                            error?.ErrorCode,
                            error?.ErrorType);
                    }
                    catch
                    {
                        return PlaidApiResponse<T>.Fail($"Plaid API returned {(int)response.StatusCode}");
                    }
                }

                var data = JsonSerializer.Deserialize<T>(body, _jsonOptions);
                return PlaidApiResponse<T>.Ok(data);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling Plaid {Endpoint}", endpoint);
                return PlaidApiResponse<T>.Fail($"Network error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout calling Plaid {Endpoint}", endpoint);
                return PlaidApiResponse<T>.Fail("Plaid API request timed out");
            }
        }
    }

    // =============================================
    // PLAID API RESPONSE WRAPPER
    // =============================================

    /// <summary>
    /// Generic response wrapper for Plaid API calls.
    /// Encapsulates success/failure without throwing exceptions.
    /// </summary>
    public class PlaidApiResponse<T> where T : class
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorType { get; set; }

        public static PlaidApiResponse<T> Ok(T data) => new() { IsSuccess = true, Data = data };
        public static PlaidApiResponse<T> Fail(string message, string code = null, string type = null) =>
            new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code, ErrorType = type };
    }

    // =============================================
    // PLAID API DATA MODELS (deserialized from JSON)
    // =============================================

    public class PlaidErrorBody
    {
        public string ErrorType { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string DisplayMessage { get; set; }
    }

    public class PlaidLinkTokenData
    {
        public string LinkToken { get; set; }
        public string Expiration { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidExchangeData
    {
        public string AccessToken { get; set; }
        public string ItemId { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidAccountsData
    {
        public PlaidApiAccount[] Accounts { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidApiAccount
    {
        public string AccountId { get; set; }
        public string Name { get; set; }
        public string OfficialName { get; set; }
        public string Mask { get; set; }
        public string Type { get; set; }
        public string Subtype { get; set; }
        public PlaidApiBalance Balances { get; set; }
    }

    public class PlaidApiBalance
    {
        public decimal? Available { get; set; }
        public decimal? Current { get; set; }
        public decimal? Limit { get; set; }
        public string IsoCurrencyCode { get; set; }
    }

    public class PlaidTransactionsData
    {
        public PlaidApiAccount[] Accounts { get; set; }
        public PlaidApiTransaction[] Transactions { get; set; }
        public int TotalTransactions { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidApiTransaction
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

    public class PlaidAuthData
    {
        public PlaidApiAccount[] Accounts { get; set; }
        public PlaidAuthNumbers Numbers { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidAuthNumbers
    {
        public PlaidAchNumber[] Ach { get; set; }
    }

    public class PlaidAchNumber
    {
        public string AccountId { get; set; }
        public string Account { get; set; }
        public string Routing { get; set; }
        public string WireRouting { get; set; }
    }

    public class PlaidIdentityData
    {
        public PlaidIdentityAccount[] Accounts { get; set; }
        public string RequestId { get; set; }
    }

    public class PlaidIdentityAccount
    {
        public string AccountId { get; set; }
        public PlaidOwner[] Owners { get; set; }
    }

    public class PlaidOwner
    {
        public string[] Names { get; set; }
        public PlaidEmail[] Emails { get; set; }
        public PlaidPhone[] PhoneNumbers { get; set; }
    }

    public class PlaidEmail
    {
        public string Data { get; set; }
        public bool Primary { get; set; }
        public string Type { get; set; }
    }

    public class PlaidPhone
    {
        public string Data { get; set; }
        public bool Primary { get; set; }
        public string Type { get; set; }
    }

    public class PlaidRemoveData
    {
        public string RequestId { get; set; }
    }
}
