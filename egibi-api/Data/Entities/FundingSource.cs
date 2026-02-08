#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Represents a user's funding source — the bank or payment provider
    /// used to deposit/withdraw fiat currency.
    ///
    /// Each user can have one primary funding source at a time.
    /// Credentials are stored separately in UserCredential (encrypted).
    /// </summary>
    public class FundingSource : EntityBase
    {
        // =============================================
        // RELATIONSHIPS
        // =============================================

        /// <summary>
        /// The user who owns this funding source.
        /// </summary>
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }

        /// <summary>
        /// The funding provider connection (e.g., Mercury, Plaid).
        /// </summary>
        [Required]
        public int ConnectionId { get; set; }

        [ForeignKey(nameof(ConnectionId))]
        public virtual Connection Connection { get; set; }

        // =============================================
        // FUNDING DETAILS
        // =============================================

        /// <summary>
        /// Whether this is the user's primary (active) funding source.
        /// Only one funding source per user should be primary.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Optional override of the provider's default base URL.
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// How this funding source was linked: "api_key" or "plaid_link".
        /// </summary>
        [MaxLength(50)]
        public string LinkMethod { get; set; }

        /// <summary>
        /// Plaid Item ID (only for plaid_link sources).
        /// </summary>
        public string PlaidItemId { get; set; }

        /// <summary>
        /// Plaid Access Token (encrypted, only for plaid_link sources).
        /// </summary>
        public string EncryptedPlaidAccessToken { get; set; }

        /// <summary>
        /// Plaid Account ID selected by the user (only for plaid_link sources).
        /// </summary>
        public string PlaidAccountId { get; set; }

        /// <summary>
        /// Display name of the linked bank account (e.g., "Chase Checking ****1234").
        /// </summary>
        [MaxLength(200)]
        public string PlaidAccountName { get; set; }

        /// <summary>
        /// Masked account number from Plaid (e.g., "****1234").
        /// </summary>
        [MaxLength(50)]
        public string PlaidAccountMask { get; set; }
    }
}
