#nullable disable
using System.ComponentModel.DataAnnotations;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Application user with authentication and encrypted key storage.
    /// Each user gets a unique Data Encryption Key (DEK) that encrypts their secrets.
    /// The DEK itself is encrypted by the application's master key.
    /// </summary>
    public class AppUser : EntityBase
    {
        [Required]
        [MaxLength(256)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// User role: "admin", "user", etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "user";

        /// <summary>
        /// Bcrypt hash of the user's login password. ONE-WAY — cannot be decrypted.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// The user's Data Encryption Key (DEK), encrypted with the application master key.
        /// Used to encrypt/decrypt this user's secrets (API keys, credentials, etc.).
        /// Generated once during account creation via IEncryptionService.GenerateUserKey().
        /// </summary>
        [Required]
        public string EncryptedDataKey { get; set; }

        /// <summary>
        /// Tracks which version of the master key encrypted this DEK.
        /// Enables incremental key rotation without downtime.
        /// </summary>
        public int KeyVersion { get; set; } = 1;

        /// <summary>
        /// Navigation: all credential sets belonging to this user.
        /// </summary>
        public virtual ICollection<UserCredential> Credentials { get; set; }
    }
}
