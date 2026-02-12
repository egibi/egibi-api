#nullable disable
using System.ComponentModel.DataAnnotations;

namespace egibi_api.Data.Entities
{
    /// <summary>
    /// Stores pending signup requests that require email verification and
    /// admin approval before a full AppUser account is created.
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
        [MaxLength(30)]
        public string Status { get; set; } = "pending_verification";

        /// <summary>
        /// SHA-256 hash of the email verification token.
        /// Null after verification is complete.
        /// </summary>
        public string EmailVerificationToken { get; set; }

        /// <summary>
        /// When the verification token expires (24 hours after creation).
        /// </summary>
        public DateTime? EmailVerificationExpiresAt { get; set; }

        /// <summary>
        /// When the user verified their email address.
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }

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

        public DateTime CreatedAt { get; set; }
    }
}
