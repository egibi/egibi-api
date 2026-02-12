using System.Security.Claims;
using egibi_api.Authorization;
using egibi_api.Data;
using egibi_api.Services;
using EgibiCoreLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace egibi_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = Policies.RequireAdmin)]
    public class UserManagementController : ControllerBase
    {
        private readonly EgibiDbContext _db;
        private readonly AppUserService _userService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            EgibiDbContext db,
            AppUserService userService,
            ILogger<UserManagementController> logger)
        {
            _db = db;
            _userService = userService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var sub = User.FindFirstValue(Claims.Subject);
            return int.Parse(sub ?? throw new UnauthorizedAccessException("No subject claim found"));
        }

        private string GetCurrentUserEmail()
        {
            return User.FindFirstValue(Claims.Email) ?? "admin";
        }

        // =============================================
        // LIST USERS
        // =============================================

        /// <summary>
        /// Returns all users (active and inactive) with summary info.
        /// </summary>
        [HttpGet("users")]
        public async Task<RequestResponse> GetUsers()
        {
            try
            {
                var users = await _db.AppUsers
                    .OrderBy(u => u.Email)
                    .Select(u => new UserSummaryDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        IsApproved = u.IsApproved,
                        ApprovedAt = u.ApprovedAt,
                        ApprovedBy = u.ApprovedBy,
                        RejectedAt = u.RejectedAt,
                        RejectedBy = u.RejectedBy,
                        RejectionReason = u.RejectionReason,
                        CreatedAt = u.CreatedAt,
                        LastModifiedAt = u.LastModifiedAt
                    })
                    .ToListAsync();

                return new RequestResponse(users, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users");
                return new RequestResponse(null, 500, "Failed to retrieve users", new ResponseError(ex));
            }
        }

        // =============================================
        // GET SINGLE USER
        // =============================================

        [HttpGet("users/{id}")]
        public async Task<RequestResponse> GetUser(int id)
        {
            try
            {
                var user = await _db.AppUsers
                    .Where(u => u.Id == id)
                    .Select(u => new UserSummaryDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        IsApproved = u.IsApproved,
                        ApprovedAt = u.ApprovedAt,
                        ApprovedBy = u.ApprovedBy,
                        RejectedAt = u.RejectedAt,
                        RejectedBy = u.RejectedBy,
                        RejectionReason = u.RejectionReason,
                        CreatedAt = u.CreatedAt,
                        LastModifiedAt = u.LastModifiedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return new RequestResponse(null, 404, "User not found");

                return new RequestResponse(user, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user {UserId}", id);
                return new RequestResponse(null, 500, "Failed to retrieve user", new ResponseError(ex));
            }
        }

        // =============================================
        // CREATE USER (admin-initiated — auto-approved)
        // =============================================

        [HttpPost("users")]
        public async Task<RequestResponse> CreateUser([FromBody] AdminCreateUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Password))
                    return new RequestResponse(null, 400, "Email and password are required");

                // Validate role
                var role = (request.Role ?? UserRoles.User).ToLower();
                if (!UserRoles.IsValid(role))
                    return new RequestResponse(null, 400, $"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", UserRoles.All)}");

                // Admin users don't need first/last name
                var firstName = role == UserRoles.Admin ? null : request.FirstName;
                var lastName = role == UserRoles.Admin ? null : request.LastName;

                var (user, error) = await _userService.CreateUserAsync(
                    request.Email, request.Password, firstName, lastName);

                if (user == null)
                    return new RequestResponse(null, 400, error);

                // Set role (CreateUserAsync defaults to "user")
                if (role != UserRoles.User)
                {
                    user.Role = role;
                }

                // Admin-created users are auto-approved
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedBy = GetCurrentUserEmail();

                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin created user {Email} with role {Role} (auto-approved)", user.Email, role);

                return new RequestResponse(new UserSummaryDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    IsApproved = user.IsApproved,
                    ApprovedAt = user.ApprovedAt,
                    ApprovedBy = user.ApprovedBy,
                    CreatedAt = user.CreatedAt
                }, 201, "User created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user");
                return new RequestResponse(null, 500, "Failed to create user", new ResponseError(ex));
            }
        }

        // =============================================
        // UPDATE USER
        // =============================================

        [HttpPut("users/{id}")]
        public async Task<RequestResponse> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request)
        {
            try
            {
                var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new RequestResponse(null, 404, "User not found");

                // Prevent admin from changing their own role
                var currentUserId = GetCurrentUserId();
                if (id == currentUserId && request.Role != null && request.Role.ToLower() != user.Role)
                    return new RequestResponse(null, 400, "You cannot change your own role");

                // Determine the effective role (new or existing)
                var effectiveRole = user.Role;
                if (request.Role != null)
                {
                    var role = request.Role.ToLower();
                    if (!UserRoles.IsValid(role))
                        return new RequestResponse(null, 400, $"Invalid role '{request.Role}'. Valid roles: {string.Join(", ", UserRoles.All)}");
                    effectiveRole = role;
                    user.Role = role;
                }

                // Admin users don't have first/last name
                if (effectiveRole == UserRoles.Admin)
                {
                    user.FirstName = null;
                    user.LastName = null;
                }
                else
                {
                    if (request.FirstName != null)
                        user.FirstName = request.FirstName.Trim();

                    if (request.LastName != null)
                        user.LastName = request.LastName.Trim();
                }

                user.LastModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin updated user {UserId}", id);

                return new RequestResponse(new UserSummaryDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    IsApproved = user.IsApproved,
                    ApprovedAt = user.ApprovedAt,
                    ApprovedBy = user.ApprovedBy,
                    CreatedAt = user.CreatedAt,
                    LastModifiedAt = user.LastModifiedAt
                }, 200, "User updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user {UserId}", id);
                return new RequestResponse(null, 500, "Failed to update user", new ResponseError(ex));
            }
        }

        // =============================================
        // DEACTIVATE / REACTIVATE USER
        // =============================================

        [HttpPost("users/{id}/deactivate")]
        public async Task<RequestResponse> DeactivateUser(int id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (id == currentUserId)
                    return new RequestResponse(null, 400, "You cannot deactivate your own account");

                var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new RequestResponse(null, 404, "User not found");

                user.IsActive = false;
                user.LastModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin deactivated user {UserId} ({Email})", id, user.Email);
                return new RequestResponse(null, 200, "User deactivated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deactivate user {UserId}", id);
                return new RequestResponse(null, 500, "Failed to deactivate user", new ResponseError(ex));
            }
        }

        [HttpPost("users/{id}/reactivate")]
        public async Task<RequestResponse> ReactivateUser(int id)
        {
            try
            {
                var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new RequestResponse(null, 404, "User not found");

                user.IsActive = true;
                user.LastModifiedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin reactivated user {UserId} ({Email})", id, user.Email);
                return new RequestResponse(null, 200, "User reactivated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reactivate user {UserId}", id);
                return new RequestResponse(null, 500, "Failed to reactivate user", new ResponseError(ex));
            }
        }

        // =============================================
        // RESET USER PASSWORD (admin-initiated)
        // =============================================

        [HttpPost("users/{id}/reset-password")]
        public async Task<RequestResponse> ResetPassword(int id, [FromBody] AdminResetPasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.NewPassword))
                    return new RequestResponse(null, 400, "New password is required");

                var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                    return new RequestResponse(null, 404, "User not found");

                var encryption = HttpContext.RequestServices.GetRequiredService<Services.Security.IEncryptionService>();
                user.PasswordHash = encryption.HashPassword(request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.LastModifiedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Admin reset password for user {UserId} ({Email})", id, user.Email);
                return new RequestResponse(null, 200, "Password reset");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for user {UserId}", id);
                return new RequestResponse(null, 500, "Failed to reset password", new ResponseError(ex));
            }
        }

        // =============================================
        // ACCOUNT APPROVAL
        // =============================================

        /// <summary>
        /// Returns all users that are pending admin approval.
        /// </summary>
        [HttpGet("pending-users")]
        public async Task<RequestResponse> GetPendingUsers()
        {
            try
            {
                var pending = await _userService.GetPendingUsersAsync();

                var dtos = pending.Select(u => new PendingUserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    CreatedAt = u.CreatedAt
                }).ToList();

                return new RequestResponse(dtos, 200, "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending users");
                return new RequestResponse(null, 500, "Failed to retrieve pending users", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Approves a pending user account.
        /// </summary>
        [HttpPost("approve-user")]
        public async Task<RequestResponse> ApproveUser([FromBody] ApproveUserRequest request)
        {
            try
            {
                if (request?.UserId <= 0)
                    return new RequestResponse(null, 400, "Valid UserId is required");

                var adminEmail = GetCurrentUserEmail();
                var (success, error) = await _userService.ApproveUserAsync(request.UserId, adminEmail);

                if (!success)
                    return new RequestResponse(null, 400, error);

                return new RequestResponse(null, 200, "User approved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve user {UserId}", request?.UserId);
                return new RequestResponse(null, 500, "Failed to approve user", new ResponseError(ex));
            }
        }

        /// <summary>
        /// Rejects a pending user account.
        /// </summary>
        [HttpPost("reject-user")]
        public async Task<RequestResponse> RejectUser([FromBody] RejectUserRequest request)
        {
            try
            {
                if (request?.UserId <= 0)
                    return new RequestResponse(null, 400, "Valid UserId is required");

                var adminEmail = GetCurrentUserEmail();
                var (success, error) = await _userService.RejectUserAsync(
                    request.UserId, adminEmail, request.Reason);

                if (!success)
                    return new RequestResponse(null, 400, error);

                return new RequestResponse(null, 200, "User rejected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject user {UserId}", request?.UserId);
                return new RequestResponse(null, 500, "Failed to reject user", new ResponseError(ex));
            }
        }
    }

    // =============================================
    // DTOs
    // =============================================

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectedBy { get; set; }
        public string RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }

    public class PendingUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApproveUserRequest
    {
        public int UserId { get; set; }
    }

    public class RejectUserRequest
    {
        public int UserId { get; set; }
        public string Reason { get; set; }
    }

    public class AdminCreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }

    public class AdminUpdateUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
    }

    public class AdminResetPasswordRequest
    {
        public string NewPassword { get; set; }
    }
}
