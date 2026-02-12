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

        // =============================================
        // PASSWORD RESET
        // =============================================

        /// <summary>
        /// Short-lived token for password reset. Hashed before storage.
        /// Null when no reset is pending.
        /// </summary>
        [MaxLength(512)]
        public string PasswordResetToken { get; set; }

        /// <summary>
        /// When the password reset token expires (UTC).
        /// </summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }

        // =============================================
        // MFA (TOTP)
        // =============================================

        /// <summary>
        /// Whether MFA is enabled for this user.
        /// When true, login requires a valid TOTP code after password verification.
        /// </summary>
        public bool IsMfaEnabled { get; set; } = false;

        /// <summary>
        /// Base32-encoded TOTP secret key, encrypted with the user's DEK.
        /// Null when MFA has not been set up.
        /// Used by authenticator apps (Google Authenticator, Authy, etc.) to generate 6-digit codes.
        /// </summary>
        [MaxLength(512)]
        public string EncryptedTotpSecret { get; set; }

        /// <summary>
        /// JSON array of one-time recovery codes, encrypted with the user's DEK.
        /// Each code can only be used once. Generated during MFA setup.
        /// Example: ["ABC12345","DEF67890",...]
        /// </summary>
        public string EncryptedRecoveryCodes { get; set; }

        // =============================================
        // ACCOUNT APPROVAL
        // =============================================

        /// <summary>
        /// Whether the account has been approved by an administrator.
        /// New self-registered accounts default to false. Admin-created and seeded accounts are auto-approved.
        /// Users cannot log in until this is true.
        /// </summary>
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// UTC timestamp when the account was approved.
        /// </summary>
        public DateTime? ApprovedAt { get; set; }

        /// <summary>
        /// Email or identifier of the admin who approved the account.
        /// "System" for auto-approved accounts (admin seed, admin-created users).
        /// </summary>
        [MaxLength(256)]
        public string ApprovedBy { get; set; }

        /// <summary>
        /// UTC timestamp when the account was rejected (if applicable).
        /// </summary>
        public DateTime? RejectedAt { get; set; }

        /// <summary>
        /// Email or identifier of the admin who rejected the account.
        /// </summary>
        [MaxLength(256)]
        public string RejectedBy { get; set; }

        /// <summary>
        /// Optional reason provided by the admin when rejecting an account.
        /// </summary>
        [MaxLength(1000)]
        public string RejectionReason { get; set; }

        /// <summary>
        /// Navigation: all credential sets belonging to this user.
        /// </summary>
        public virtual ICollection<UserCredential> Credentials { get; set; }
    }
}
