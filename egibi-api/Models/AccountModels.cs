using System.ComponentModel.DataAnnotations;

namespace egibi_api.Models
{
    // =============================================
    // CREATE ACCOUNT REQUEST
    // =============================================

    /// <summary>
    /// Request body for creating a new account with optional credentials.
    /// Handles both service-linked and custom accounts.
    /// </summary>
    public class CreateAccountRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Connection (service) ID. Null for custom accounts.
        /// </summary>
        public int? ConnectionId { get; set; }

        /// <summary>
        /// Account type ID (from AccountType reference table).
        /// </summary>
        public int? AccountTypeId { get; set; }

        /// <summary>
        /// Optional credential fields. Only the fields required by the
        /// selected Connection need to be populated.
        /// All values are plaintext here — the API encrypts them before storage.
        /// </summary>
        public AccountCredentials? Credentials { get; set; }

        /// <summary>
        /// Optional base URL override (defaults to Connection.DefaultBaseUrl).
        /// </summary>
        public string? BaseUrl { get; set; }
    }

    /// <summary>
    /// Plaintext credential values submitted during account creation.
    /// The API encrypts these with the user's DEK before storing in UserCredential.
    /// </summary>
    public class AccountCredentials
    {
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? Passphrase { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        /// <summary>
        /// User-friendly label for this credential set.
        /// e.g., "My Coinbase Trading Key"
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// What permissions does this key have?
        /// e.g., "read,trade" or "read-only"
        /// </summary>
        public string? Permissions { get; set; }
    }

    // =============================================
    // ACCOUNT RESPONSE (for list views)
    // =============================================

    /// <summary>
    /// Account data returned to the frontend for list views.
    /// Credentials are NEVER returned in plaintext — only masked hints.
    /// </summary>
    public class AccountResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        // Service info
        public int? ConnectionId { get; set; }
        public string ConnectionName { get; set; }
        public string ConnectionIconKey { get; set; }
        public string ConnectionColor { get; set; }
        public string ConnectionCategory { get; set; }

        // Account type
        public int? AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }

        // Credential summary (never plaintext)
        public bool HasCredentials { get; set; }
        public string CredentialLabel { get; set; }
        public string MaskedApiKey { get; set; }
        public DateTime? CredentialLastUsedAt { get; set; }

        // Funding
        public bool IsPrimaryFunding { get; set; }
    }

    // =============================================
    // ACCOUNT DETAIL RESPONSE (for detail view)
    // =============================================

    /// <summary>
    /// Full account detail returned for the account detail page.
    /// Includes connection metadata, credential summary, and fee structure.
    /// </summary>
    public class AccountDetailResponse
    {
        // General
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        // Connection / service info
        public int? ConnectionId { get; set; }
        public string ConnectionName { get; set; }
        public string ConnectionIconKey { get; set; }
        public string ConnectionColor { get; set; }
        public string ConnectionCategory { get; set; }
        public string ConnectionWebsite { get; set; }
        public string BaseUrl { get; set; }
        public string[] RequiredFields { get; set; }

        // Account type
        public int? AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }

        // Credential summary (never plaintext)
        public CredentialSummary Credentials { get; set; }

        // Fee structure
        public AccountFeeDetail Fees { get; set; }
    }

    /// <summary>
    /// Masked credential summary — plaintext values are NEVER returned.
    /// </summary>
    public class CredentialSummary
    {
        public bool HasCredentials { get; set; }
        public string Label { get; set; }
        public string MaskedApiKey { get; set; }
        public string MaskedApiSecret { get; set; }
        public bool HasPassphrase { get; set; }
        public bool HasUsername { get; set; }
        public string Permissions { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Fee structure data for display and editing.
    /// </summary>
    public class AccountFeeDetail
    {
        public int? Id { get; set; }
        public decimal MakerFeePercent { get; set; }
        public decimal TakerFeePercent { get; set; }
        public string FeeScheduleType { get; set; }
        public string Notes { get; set; }
    }

    // =============================================
    // UPDATE ACCOUNT REQUEST
    // =============================================

    /// <summary>
    /// Request body for updating general account properties.
    /// </summary>
    public class UpdateAccountRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }
        public string? Notes { get; set; }

        public int? AccountTypeId { get; set; }
        public bool IsActive { get; set; }
    }

    // =============================================
    // UPDATE CREDENTIALS REQUEST
    // =============================================

    /// <summary>
    /// Request body for updating encrypted credentials.
    /// All plaintext values are encrypted before storage.
    /// Only non-null fields are updated — pass null to keep existing value.
    /// </summary>
    public class UpdateCredentialsRequest
    {
        [Required]
        public int AccountId { get; set; }

        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }
        public string? Passphrase { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Label { get; set; }
        public string? Permissions { get; set; }
        public string? BaseUrl { get; set; }
    }

    // =============================================
    // UPDATE FEES REQUEST
    // =============================================

    /// <summary>
    /// Request body for updating account fee structure.
    /// </summary>
    public class UpdateAccountFeesRequest
    {
        [Required]
        public int AccountId { get; set; }

        public decimal MakerFeePercent { get; set; }
        public decimal TakerFeePercent { get; set; }

        [MaxLength(50)]
        public string FeeScheduleType { get; set; } = "flat";

        public string? Notes { get; set; }
    }

    // =============================================
    // TEST CONNECTION
    // =============================================

    /// <summary>
    /// Response from testing an account's API connection.
    /// </summary>
    public class TestConnectionResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public string Message { get; set; }
        public DateTime TestedAt { get; set; }
    }

    // =============================================
    // CONNECTION / SERVICE CATALOG RESPONSE
    // =============================================

    /// <summary>
    /// Service catalog entry returned for the card picker.
    /// </summary>
    public class ServiceCatalogEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string IconKey { get; set; }
        public string Color { get; set; }
        public string Website { get; set; }
        public string DefaultBaseUrl { get; set; }
        public string[] RequiredFields { get; set; }
        public bool IsActive { get; set; }
        public bool IsDataSource { get; set; }
        public int SortOrder { get; set; }
    }
}
