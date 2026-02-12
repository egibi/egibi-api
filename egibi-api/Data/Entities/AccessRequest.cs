#nullable disable
using System.ComponentModel.DataAnnotations;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Stores pending signup requests that require admin approval before
    /// a full AppUser account is created.
    /// </summary>
    public class AccessRequest
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        /// <summary>
        /// Bcrypt hash of the requested password (hashed at request time so the
        /// raw password is never stored). When approved, this becomes the user's PasswordHash.
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// pending_verification | pending | approved | denied
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Optional reason when an admin denies the request.
        /// </summary>
        [MaxLength(500)]
        public string DenialReason { get; set; }

        /// <summary>
        /// Id of the admin who reviewed the request (null while pending).
        /// </summary>
        public int? ReviewedByUserId { get; set; }

        /// <summary>
        /// When the admin approved or denied this request.
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// IP address of the client that submitted the request.
        /// Supports both IPv4 and IPv6 (max 45 chars).
        /// </summary>
        [MaxLength(45)]
        public string IpAddress { get; set; }

        // =============================================
        // EMAIL VERIFICATION FIELDS
        // =============================================

        /// <summary>
        /// SHA-256 hash of the email verification token.
        /// Raw token is sent via email; only the hash is stored.
        /// </summary>
        public string EmailVerificationToken { get; set; }

        /// <summary>
        /// When the email verification token expires (UTC).
        /// </summary>
        public DateTime? EmailVerificationExpiresAt { get; set; }

        /// <summary>
        /// When the user successfully verified their email (UTC).
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
