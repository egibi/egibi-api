#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Stores a user's Plaid developer credentials (encrypted).
    /// Each user manages their own Plaid integration via dashboard.plaid.com.
    /// One config per user — credentials are encrypted with the user's DEK.
    /// </summary>
    public class UserPlaidConfig
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The user who owns this Plaid configuration.
        /// </summary>
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }

        // =============================================
        // ENCRYPTED FIELDS
        // =============================================

        /// <summary>
        /// Encrypted Plaid Client ID (from dashboard.plaid.com).
        /// </summary>
        [Required]
        public string EncryptedClientId { get; set; }

        /// <summary>
        /// Encrypted Plaid Secret (from dashboard.plaid.com).
        /// </summary>
        [Required]
        public string EncryptedSecret { get; set; }

        // =============================================
        // CONFIGURATION
        // =============================================

        /// <summary>
        /// Plaid environment: "sandbox", "development", or "production".
        /// Determines the base URL for API calls.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Environment { get; set; } = "sandbox";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }
}
