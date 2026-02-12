#nullable disable
using System.ComponentModel.DataAnnotations;

namespace egibi_api.Models
{
    // =============================================
    // ACCESS REQUEST MODELS
    // =============================================

    /// <summary>
    /// Public endpoint model — submitted by unauthenticated users to request access.
    /// </summary>
    public class RequestAccessModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        /// <summary>
        /// Optional message explaining why the user wants access.
        /// </summary>
        [MaxLength(1000)]
        public string Message { get; set; }
    }

    // =============================================
    // ADMIN REVIEW MODELS
    // =============================================

    /// <summary>
    /// Admin action to approve a pending access request. Creates the AppUser.
    /// </summary>
    public class ApproveAccessRequestModel
    {
        [Required]
        public int RequestId { get; set; }
    }

    /// <summary>
    /// Admin action to reject a pending access request.
    /// </summary>
    public class RejectAccessRequestModel
    {
        [Required]
        public int RequestId { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; }
    }

    // =============================================
    // RESPONSE DTOs
    // =============================================

    /// <summary>
    /// Returned to admin when listing pending access requests.
    /// </summary>
    public class AccessRequestDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedBy { get; set; }
        public string RejectionReason { get; set; }
    }
}
