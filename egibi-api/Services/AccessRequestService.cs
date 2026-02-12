#nullable disable
using System.Security.Cryptography;
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Services.Email;
using egibi_api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services
{
    public class AccessRequestService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly AppUserService _userService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly ILogger<AccessRequestService> _logger;

        public AccessRequestService(
            EgibiDbContext db,
            IEncryptionService encryption,
            AppUserService userService,
            IEmailService emailService,
            IConfiguration config,
            ILogger<AccessRequestService> logger)
        {
            _db = db;
            _encryption = encryption;
            _userService = userService;
            _emailService = emailService;
            _config = config;
            _logger = logger;
        }

        // =============================================
        // SUBMIT REQUEST
        // =============================================

        /// <summary>
        /// Creates a new access request with status "pending_verification".
        /// Generates an email verification token and sends a verification email.
        /// Returns error if the email already has a pending request or an active account.
        /// </summary>
        public async Task<(AccessRequest Request, string Error)> SubmitRequestAsync(
            string email, string password, string firstName, string lastName)
        {
            email = email.Trim().ToLowerInvariant();

            // Check for existing active account
            var existingUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
                return (null, "An account with this email already exists.");

            // Check for existing pending or pending_verification request
            var existingRequest = await _db.AccessRequests
                .FirstOrDefaultAsync(r => r.Email == email
                    && (r.Status == "pending" || r.Status == "pending_verification"));
            if (existingRequest != null)
                return (null, "An access request for this email is already pending.");

            // Generate a cryptographically random verification token
            var rawToken = GenerateSecureToken();
            var tokenHash = HashToken(rawToken);

            var request = new AccessRequest
            {
                Email = email,
                FirstName = firstName?.Trim(),
                LastName = lastName?.Trim(),
                PasswordHash = _encryption.HashPassword(password),
                Status = "pending_verification",
                EmailVerificationToken = tokenHash,
                EmailVerificationExpiresAt = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            await _db.AccessRequests.AddAsync(request);
            await _db.SaveChangesAsync();

            // Build verification link and send email (non-blocking on failure)
            var frontendUrl = _config["App:FrontendUrl"] ?? "https://www.egibi.io";
            var verificationLink = $"{frontendUrl}/auth/verify-email?token={rawToken}&email={Uri.EscapeDataString(email)}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailVerificationAsync(email, firstName?.Trim(), verificationLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email for {Email}", email);
                }
            });

            _logger.LogInformation("Access request submitted for {Email} (pending verification)", email);
            return (request, null);
        }

        // =============================================
        // VERIFY EMAIL
        // =============================================

        /// <summary>
        /// Verifies an email address using the token sent via email.
        /// Transitions the request from "pending_verification" to "pending".
        /// </summary>
        public async Task<(bool Success, string Error)> VerifyEmailAsync(string email, string token)
        {
            email = email.Trim().ToLowerInvariant();
            var tokenHash = HashToken(token);

            var request = await _db.AccessRequests
                .FirstOrDefaultAsync(r => r.Email == email && r.Status == "pending_verification");

            if (request == null)
                return (false, "No pending verification found for this email.");

            // Check token expiry
            if (request.EmailVerificationExpiresAt.HasValue
                && DateTime.UtcNow > request.EmailVerificationExpiresAt.Value)
            {
                return (false, "Verification link has expired. Please request access again.");
            }

            // Compare token hashes (constant-time comparison)
            if (!CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(tokenHash),
                    System.Text.Encoding.UTF8.GetBytes(request.EmailVerificationToken ?? "")))
            {
                return (false, "Invalid verification token.");
            }

            // Mark email as verified, promote to "pending" for admin review
            request.Status = "pending";
            request.EmailVerifiedAt = DateTime.UtcNow;
            request.EmailVerificationToken = null; // Clear token after use
            request.EmailVerificationExpiresAt = null;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Email verified for access request {Email}", email);
            return (true, null);
        }

        // =============================================
        // RESEND VERIFICATION EMAIL
        // =============================================

        /// <summary>
        /// Resends the verification email for a pending_verification request.
        /// Generates a new token and extends the expiry.
        /// </summary>
        public async Task<(bool Success, string Error)> ResendVerificationAsync(string email)
        {
            email = email.Trim().ToLowerInvariant();

            var request = await _db.AccessRequests
                .FirstOrDefaultAsync(r => r.Email == email && r.Status == "pending_verification");

            if (request == null)
                return (false, "No pending verification found for this email.");

            // Generate a new token
            var rawToken = GenerateSecureToken();
            request.EmailVerificationToken = HashToken(rawToken);
            request.EmailVerificationExpiresAt = DateTime.UtcNow.AddHours(24);

            await _db.SaveChangesAsync();

            // Send new verification email
            var frontendUrl = _config["App:FrontendUrl"] ?? "https://www.egibi.io";
            var verificationLink = $"{frontendUrl}/auth/verify-email?token={rawToken}&email={Uri.EscapeDataString(email)}";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendEmailVerificationAsync(email, request.FirstName, verificationLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resend verification email for {Email}", email);
                }
            });

            _logger.LogInformation("Verification email resent for {Email}", email);
            return (true, null);
        }

        // =============================================
        // ADMIN: GET PENDING REQUESTS
        // =============================================

        public async Task<List<AccessRequest>> GetPendingRequestsAsync()
        {
            return await _db.AccessRequests
                .Where(r => r.Status == "pending")
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();
        }

        // =============================================
        // ADMIN: GET ALL REQUESTS
        // =============================================

        public async Task<List<AccessRequest>> GetAllRequestsAsync()
        {
            return await _db.AccessRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // =============================================
        // ADMIN: APPROVE REQUEST
        // =============================================

        /// <summary>
        /// Approves a pending access request by creating the real AppUser account
        /// and marking the request as approved. Sends an approval notification email.
        /// </summary>
        public async Task<(AppUser User, string Error)> ApproveRequestAsync(int requestId, int adminUserId)
        {
            var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
                return (null, "Access request not found.");

            if (request.Status != "pending")
                return (null, $"Request has already been {request.Status}.");

            // Check if a user with this email was created in the meantime
            var existingUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                request.Status = "approved";
                request.ReviewedByUserId = adminUserId;
                request.ReviewedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return (null, "An account with this email already exists. Request marked as approved.");
            }

            // Create the real user account
            var user = new AppUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = "user",
                PasswordHash = request.PasswordHash, // Already hashed at submission time
                EncryptedDataKey = _encryption.GenerateUserKey(),
                KeyVersion = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _db.AppUsers.AddAsync(user);

            request.Status = "approved";
            request.ReviewedByUserId = adminUserId;
            request.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Access request {RequestId} approved by admin {AdminId}. User {Email} created.",
                requestId, adminUserId, user.Email);

            // Send approval notification email (non-blocking)
            var frontendUrl = _config["App:FrontendUrl"] ?? "https://www.egibi.io";
            var loginUrl = $"{frontendUrl}/auth/login";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendAccessApprovedAsync(request.Email, request.FirstName, loginUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval email for {Email}", request.Email);
                }
            });

            return (user, null);
        }

        // =============================================
        // ADMIN: DENY REQUEST
        // =============================================

        public async Task<(bool Success, string Error)> DenyRequestAsync(
            int requestId, int adminUserId, string reason = null)
        {
            var request = await _db.AccessRequests.FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
                return (false, "Access request not found.");

            if (request.Status != "pending")
                return (false, $"Request has already been {request.Status}.");

            request.Status = "denied";
            request.DenialReason = reason;
            request.ReviewedByUserId = adminUserId;
            request.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Access request {RequestId} denied by admin {AdminId}. Email: {Email}",
                requestId, adminUserId, request.Email);

            // Send denial notification email (non-blocking)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendAccessDeniedAsync(request.Email, request.FirstName, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send denial email for {Email}", request.Email);
                }
            });

            return (true, null);
        }

        // =============================================
        // TOKEN HELPERS
        // =============================================

        /// <summary>
        /// Generates a URL-safe cryptographic random token (64 characters).
        /// </summary>
        private static string GenerateSecureToken()
        {
            var bytes = new byte[48]; // 48 bytes → 64 base64url chars
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }

        /// <summary>
        /// Computes a SHA-256 hash of a token for safe storage.
        /// We never store the raw token — only the hash.
        /// </summary>
        private static string HashToken(string token)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
