using System.ComponentModel.DataAnnotations;

namespace egibi_api.Models
{
    // =============================================
    // PLAID CONFIG
    // =============================================

    /// <summary>
    /// Request to save a user's Plaid developer credentials.
    /// </summary>
    public class PlaidConfigRequest
    {
        [Required]
        public string ClientId { get; set; }

        [Required]
        public string Secret { get; set; }

        /// <summary>
        /// "sandbox", "development", or "production"
        /// </summary>
        public string Environment { get; set; } = "sandbox";
    }

    // =============================================
    // LINK TOKEN
    // =============================================

    /// <summary>
    /// Response from the create-link-token endpoint.
    /// Frontend uses link_token to initialize Plaid Link.
    /// </summary>
    public class PlaidLinkTokenResponse
    {
        public string LinkToken { get; set; }
        public string Expiration { get; set; }
    }

    // =============================================
    // TOKEN EXCHANGE
    // =============================================

    /// <summary>
    /// Request to exchange a Plaid public_token for an access_token
    /// and create the funding source account.
    /// </summary>
    public class PlaidExchangeTokenRequest
    {
        [Required]
        public string PublicToken { get; set; }

        /// <summary>
        /// The Plaid account_id the user selected as their funding account.
        /// </summary>
        [Required]
        public string SelectedAccountId { get; set; }

        /// <summary>
        /// User-friendly name for the Egibi account.
        /// </summary>
        [MaxLength(200)]
        public string AccountName { get; set; }

        /// <summary>
        /// Institution metadata returned by Plaid Link onSuccess.
        /// </summary>
        public PlaidInstitutionMeta Institution { get; set; }

        /// <summary>
        /// Account metadata returned by Plaid Link onSuccess.
        /// </summary>
        public PlaidAccountMeta[] Accounts { get; set; }
    }

    public class PlaidInstitutionMeta
    {
        public string InstitutionId { get; set; }
        public string Name { get; set; }
    }

    public class PlaidAccountMeta
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Mask { get; set; }
        public string Type { get; set; }
        public string Subtype { get; set; }
    }

    // =============================================
    // PLAID ACCOUNT RESPONSES
    // =============================================

    /// <summary>
    /// Response for a Plaid-linked funding source, returned alongside
    /// the standard FundingSourceResponse when the provider is Plaid.
    /// </summary>
    public class PlaidFundingDetails
    {
        public int PlaidItemId { get; set; }
        public string InstitutionId { get; set; }
        public string InstitutionName { get; set; }
        public DateTime? LastSyncedAt { get; set; }

        // Selected account details
        public string PlaidAccountId { get; set; }
        public string AccountName { get; set; }
        public string Mask { get; set; }
        public string AccountType { get; set; }
        public string AccountSubtype { get; set; }
        public decimal? AvailableBalance { get; set; }
        public decimal? CurrentBalance { get; set; }
        public string IsoCurrencyCode { get; set; }
        public DateTime? BalanceLastUpdatedAt { get; set; }
    }

    // =============================================
    // BALANCE RESPONSES
    // =============================================

    /// <summary>
    /// Response from refreshing Plaid balances.
    /// </summary>
    public class PlaidBalanceResponse
    {
        public string PlaidAccountId { get; set; }
        public string AccountName { get; set; }
        public string Mask { get; set; }
        public string AccountType { get; set; }
        public string AccountSubtype { get; set; }
        public decimal? AvailableBalance { get; set; }
        public decimal? CurrentBalance { get; set; }
        public string IsoCurrencyCode { get; set; }
    }

    // =============================================
    // TRANSACTION RESPONSES
    // =============================================

    public class PlaidTransactionResponse
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

    // =============================================
    // IDENTITY RESPONSES
    // =============================================

    public class PlaidIdentityResponse
    {
        public string AccountId { get; set; }
        public PlaidOwnerInfo[] Owners { get; set; }
    }

    public class PlaidOwnerInfo
    {
        public string[] Names { get; set; }
        public string[] Emails { get; set; }
        public string[] PhoneNumbers { get; set; }
    }

    // =============================================
    // AUTH RESPONSES
    // =============================================

    public class PlaidAuthResponse
    {
        public string AccountId { get; set; }
        public string AccountNumber { get; set; }
        public string RoutingNumber { get; set; }
        public string WireRoutingNumber { get; set; }
    }
}
