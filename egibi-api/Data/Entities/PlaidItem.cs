#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Represents a bank linked through Plaid Link.
    /// Stores the encrypted Plaid access_token and institution metadata.
    /// One PlaidItem per bank institution per user.
    /// </summary>
    public class PlaidItem : EntityBase
    {
        // =============================================
        // RELATIONSHIPS
        // =============================================

        /// <summary>
        /// The user who linked this bank.
        /// </summary>
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }

        /// <summary>
        /// The Egibi Account created for this funding source.
        /// </summary>
        public int? AccountId { get; set; }

        [ForeignKey(nameof(AccountId))]
        public virtual Account Account { get; set; }

        // =============================================
        // PLAID FIELDS
        // =============================================

        /// <summary>
        /// Plaid item_id — unique identifier for this linked bank.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string PlaidItemId { get; set; }

        /// <summary>
        /// Encrypted Plaid access_token (AES-256-GCM via user DEK).
        /// Used to call Plaid API endpoints for this item.
        /// Access tokens do not expire but can be revoked.
        /// </summary>
        [Required]
        public string EncryptedAccessToken { get; set; }

        /// <summary>
        /// Plaid institution_id (e.g., "ins_109508").
        /// </summary>
        [MaxLength(100)]
        public string InstitutionId { get; set; }

        /// <summary>
        /// Human-readable institution name (e.g., "Chase", "Bank of America").
        /// </summary>
        [MaxLength(200)]
        public string InstitutionName { get; set; }

        /// <summary>
        /// Plaid products enabled for this item (e.g., "auth,transactions,balance,identity").
        /// </summary>
        [MaxLength(500)]
        public string EnabledProducts { get; set; }

        /// <summary>
        /// Consent expiration date, if applicable (OAuth institutions).
        /// </summary>
        public DateTime? ConsentExpiresAt { get; set; }

        /// <summary>
        /// Last time we successfully synced data for this item.
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        // =============================================
        // NAVIGATION
        // =============================================

        public virtual ICollection<PlaidAccount> PlaidAccounts { get; set; }
    }
}
