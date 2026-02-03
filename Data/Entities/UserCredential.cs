#nullable disable
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Stores an encrypted credential set for a user's external connection.
    /// All sensitive fields (ApiKey, ApiSecret, Passphrase) are encrypted with 
    /// the user's DEK before storage — the database never sees plaintext.
    /// 
    /// One UserCredential per connection per user. For example:
    ///   - User "Adam" + Connection "Coinbase" → 1 UserCredential row
    ///   - User "Adam" + Connection "Binance US" → 1 UserCredential row
    /// </summary>
    public class UserCredential : EntityBase
    {
        // =============================================
        // RELATIONSHIPS
        // =============================================

        /// <summary>
        /// The user who owns these credentials.
        /// </summary>
        [Required]
        public int AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }

        /// <summary>
        /// The connection/service these credentials are for (e.g., "Binance US", "Coinbase").
        /// </summary>
        [Required]
        public int ConnectionId { get; set; }

        [ForeignKey(nameof(ConnectionId))]
        public virtual Connection Connection { get; set; }

        // =============================================
        // ENCRYPTED FIELDS
        // =============================================
        // All values below are AES-256-GCM ciphertext (base64).
        // Decrypt via IEncryptionService.Decrypt(value, user.EncryptedDataKey)

        /// <summary>
        /// Encrypted API key / client ID.
        /// </summary>
        public string EncryptedApiKey { get; set; }

        /// <summary>
        /// Encrypted API secret / client secret.
        /// </summary>
        public string EncryptedApiSecret { get; set; }

        /// <summary>
        /// Encrypted passphrase (required by some exchanges like Coinbase Pro).
        /// Null if not applicable.
        /// </summary>
        public string EncryptedPassphrase { get; set; }

        /// <summary>
        /// Encrypted username (if the connection uses user/pass auth instead of API keys).
        /// Null if not applicable.
        /// </summary>
        public string EncryptedUsername { get; set; }

        /// <summary>
        /// Encrypted password (if the connection uses user/pass auth).
        /// Null if not applicable.
        /// </summary>
        public string EncryptedPassword { get; set; }

        // =============================================
        // METADATA
        // =============================================

        /// <summary>
        /// User-friendly label for this credential set.
        /// e.g., "My Coinbase Trading Key", "Read-Only Binance Key"
        /// </summary>
        [MaxLength(200)]
        public string Label { get; set; }

        /// <summary>
        /// What permissions does this key have? Stored as a comma-separated string
        /// or JSON for flexibility. e.g., "read,trade" or "read-only"
        /// </summary>
        [MaxLength(500)]
        public string Permissions { get; set; }

        /// <summary>
        /// When were these credentials last used to make an API call?
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// When do these credentials expire? Null if they don't expire.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Tracks which version of the user's DEK encrypted these fields.
        /// Enables incremental credential re-encryption during key rotation.
        /// </summary>
        public int KeyVersion { get; set; } = 1;
    }
}
