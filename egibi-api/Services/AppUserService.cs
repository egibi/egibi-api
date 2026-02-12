#nullable disable
using System.Security.Cryptography;
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services
{
    public class AppUserService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<AppUserService> _logger;
        private readonly IConfiguration _configuration;

        public AppUserService(
            EgibiDbContext db,
            IEncryptionService encryption,
            ILogger<AppUserService> logger,
            IConfiguration configuration)
        {
            _db = db;
            _encryption = encryption;
            _logger = logger;
            _configuration = configuration;
        }

        // =============================================
        // USER QUERIES
        // =============================================

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<AppUser> GetByEmailAsync(string email)
        {
            return await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await _db.AppUsers.Where(u => u.IsActive).ToListAsync();
        }

        // =============================================
        // SIGNUP
        // =============================================

        /// <summary>
        /// Creates a new user account. Returns null + error message if validation fails.
        /// New self-registered accounts are NOT approved by default — an admin must approve them.
        /// </summary>
        public async Task<(AppUser User, string Error)> CreateUserAsync(
            string email, string password, string firstName, string lastName)
        {
            // Validate email uniqueness
            var existing = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
                return (null, "An account with this email already exists.");

            // Validate password strength
            var passwordError = ValidatePassword(password);
            if (passwordError != null)
                return (null, passwordError);

            var user = new AppUser
            {
                Email = email.Trim().ToLowerInvariant(),
                FirstName = firstName?.Trim(),
                LastName = lastName?.Trim(),
                Role = "user",
                PasswordHash = _encryption.HashPassword(password),
                EncryptedDataKey = _encryption.GenerateUserKey(),
                KeyVersion = 1,
                IsActive = true,
                IsApproved = false, // Requires admin approval
                CreatedAt = DateTime.UtcNow
            };

            await _db.AppUsers.AddAsync(user);
            await _db.SaveChangesAsync();

            _logger.LogInformation("New user created (pending approval): {Email}", user.Email);
            return (user, null);
        }

        // =============================================
        // ACCOUNT APPROVAL
        // =============================================

        /// <summary>
        /// Returns all users that are active but not yet approved (pending admin review).
        /// </summary>
        public async Task<List<AppUser>> GetPendingUsersAsync()
        {
            return await _db.AppUsers
                .Where(u => u.IsActive && !u.IsApproved && u.RejectedAt == null)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Approves a pending user account. Sets IsApproved = true and records who approved.
        /// </summary>
        public async Task<(bool Success, string Error)> ApproveUserAsync(int userId, string approvedByEmail)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                return (false, "User not found.");

            if (user.IsApproved)
                return (false, "User is already approved.");

            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedBy = approvedByEmail;
            user.RejectedAt = null;
            user.RejectedBy = null;
            user.RejectionReason = null;
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("User {Email} approved by {ApprovedBy}.", user.Email, approvedByEmail);
            return (true, null);
        }

        /// <summary>
        /// Rejects a pending user account. Deactivates the account and records the reason.
        /// </summary>
        public async Task<(bool Success, string Error)> RejectUserAsync(int userId, string rejectedByEmail, string reason)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
                return (false, "User not found.");

            if (user.IsApproved)
                return (false, "Cannot reject an already-approved user.");

            user.IsActive = false;
            user.RejectedAt = DateTime.UtcNow;
            user.RejectedBy = rejectedByEmail;
            user.RejectionReason = reason;
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("User {Email} rejected by {RejectedBy}. Reason: {Reason}",
                user.Email, rejectedByEmail, reason ?? "(none)");
            return (true, null);
        }

        // =============================================
        // PASSWORD RESET
        // =============================================

        /// <summary>
        /// Generates a password reset token for the given email.
        /// Returns the raw token (to be sent via email) or null if user not found.
        /// The token stored in DB is hashed for security.
        /// </summary>
        public async Task<string> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(
                u => u.Email == email && u.IsActive);

            if (user == null)
                return null; // Don't reveal if user exists

            // Generate a cryptographically random token
            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            // Hash the token before storing (like a password — never store raw tokens in DB)
            user.PasswordResetToken = _encryption.HashPassword(rawToken);
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // 1-hour expiry

            await _db.SaveChangesAsync();
            _logger.LogInformation("Password reset token generated for {Email}.", email);

            return rawToken;
        }

        /// <summary>
        /// Validates a reset token and sets the new password.
        /// Returns (success, error message).
        /// </summary>
        public async Task<(bool Success, string Error)> ResetPasswordAsync(
            string email, string token, string newPassword)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(
                u => u.Email == email && u.IsActive);

            if (user == null)
                return (false, "Invalid or expired reset link.");

            // Check token expiry
            if (user.PasswordResetToken == null || user.PasswordResetTokenExpiry == null)
                return (false, "No password reset was requested.");

            if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                // Clean up expired token
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                await _db.SaveChangesAsync();
                return (false, "Reset link has expired. Please request a new one.");
            }

            // Verify the token (it was hashed before storing)
            if (!_encryption.VerifyPassword(token, user.PasswordResetToken))
                return (false, "Invalid or expired reset link.");

            // Validate new password
            var passwordError = ValidatePassword(newPassword);
            if (passwordError != null)
                return (false, passwordError);

            // Set the new password and clear the reset token
            user.PasswordHash = _encryption.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Password reset completed for {Email}.", email);

            return (true, null);
        }

        // =============================================
        // ADMIN SEEDING
        // =============================================

        /// <summary>
        /// Ensures the default admin account exists. Called on startup.
        /// Admin accounts are auto-approved (IsApproved = true, ApprovedBy = "System").
        /// </summary>
        public async Task SeedAdminAsync()
        {
            const string adminEmail = "admin@egibi.io";

            var existing = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (existing != null)
            {
                _logger.LogInformation("Admin account already exists ({Email}).", adminEmail);
                return;
            }

            // Read from configuration — falls back to a generated password if not set
            var defaultPassword = _configuration["AdminSeed:DefaultPassword"];
            if (string.IsNullOrWhiteSpace(defaultPassword) || defaultPassword == "CHANGE_ME_ON_FIRST_LOGIN")
            {
                // Generate a random password if not configured
                defaultPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
                _logger.LogWarning(
                    "No AdminSeed:DefaultPassword configured. Generated random admin password: {Password} — save this and change after first login.",
                    defaultPassword);
            }

            var admin = new AppUser
            {
                Email = adminEmail,
                Role = "admin",
                PasswordHash = _encryption.HashPassword(defaultPassword),
                EncryptedDataKey = _encryption.GenerateUserKey(),
                KeyVersion = 1,
                IsActive = true,
                IsApproved = true,
                ApprovedAt = DateTime.UtcNow,
                ApprovedBy = "System",
                CreatedAt = DateTime.UtcNow
            };

            await _db.AppUsers.AddAsync(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin account created: {Email} (change password after first login).", adminEmail);
        }

        // =============================================
        // HELPERS
        // =============================================

        private static string ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return "Password must be at least 8 characters.";
            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";
            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter.";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number.";
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return "Password must contain at least one special character.";
            return null;
        }
    }
}
