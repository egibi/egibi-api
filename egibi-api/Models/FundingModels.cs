using System.ComponentModel.DataAnnotations;

namespace egibi_api.Models
{
    // =============================================
    // FUNDING SOURCE RESPONSE
    // =============================================

    /// <summary>
    /// Response representing the user's primary funding source.
    /// Returns masked credentials and connection metadata.
    /// </summary>
    public class FundingSourceResponse
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Connection / provider info
        public int ConnectionId { get; set; }
        public string ProviderName { get; set; }
        public string ProviderIconKey { get; set; }
        public string ProviderColor { get; set; }
        public string ProviderWebsite { get; set; }
        public string BaseUrl { get; set; }

        // Credential summary (never plaintext)
        public bool HasCredentials { get; set; }
        public string CredentialLabel { get; set; }
        public string MaskedApiKey { get; set; }
        public DateTime? CredentialLastUsedAt { get; set; }
    }

    // =============================================
    // FUNDING PROVIDER CATALOG ENTRY
    // =============================================

    /// <summary>
    /// Funding provider info returned for the provider picker.
    /// </summary>
    public class FundingProviderEntry
    {
        public int ConnectionId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public string Color { get; set; }
        public string Website { get; set; }
        public string DefaultBaseUrl { get; set; }
        public string[] RequiredFields { get; set; }
        public string SignupUrl { get; set; }
        public string ApiDocsUrl { get; set; }
    }

    // =============================================
    // CREATE FUNDING SOURCE REQUEST
    // =============================================

    /// <summary>
    /// Request body for creating or updating the primary funding source.
    /// </summary>
    public class CreateFundingSourceRequest
    {
        [Required]
        public int ConnectionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Plaintext API credentials — encrypted before storage.
        /// </summary>
        public AccountCredentials Credentials { get; set; }

        /// <summary>
        /// Optional base URL override.
        /// </summary>
        public string BaseUrl { get; set; }
    }
}
