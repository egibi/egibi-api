#nullable disable
using System.Security.Cryptography;
using System.Text.Json;
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Services.Security;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace egibi_api.Services
{
    public class MfaService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<MfaService> _logger;
        private const string Issuer = "Egibi";
        private const int RecoveryCodeCount = 8;

        public MfaService(
            EgibiDbContext db,
            IEncryptionService encryption,
            ILogger<MfaService> logger)
        {
            _db = db;
            _encryption = encryption;
            _logger = logger;
        }

        // =============================================
        // MFA SETUP
        // =============================================

        /// <summary>
        /// Generates a new TOTP secret and returns the provisioning URI for QR code display.
        /// Does NOT enable MFA yet — the user must verify a code first via ConfirmSetupAsync.
        /// The secret is stored encrypted but IsMfaEnabled remains false until confirmed.
        /// </summary>
        public async Task<(string SharedKey, string QrUri, string Error)> BeginSetupAsync(int userId)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (null, null, "User not found.");

            if (user.IsMfaEnabled)
                return (null, null, "MFA is already enabled. Disable it first to reconfigure.");

            // Generate a new 20-byte secret
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretBytes);

            // Encrypt and store the secret (MFA not yet active)
            user.EncryptedTotpSecret = _encryption.Encrypt(base32Secret, user.EncryptedDataKey);
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Build the otpauth:// URI for QR code generation
            var qrUri = new OtpUri(OtpType.Totp, secretBytes, user.Email, Issuer).ToString();

            _logger.LogInformation("MFA setup initiated for user {UserId}.", userId);
            return (base32Secret, qrUri, null);
        }

        /// <summary>
        /// Verifies the user can produce a valid TOTP code from their authenticator app,
        /// then enables MFA and generates recovery codes.
        /// </summary>
        public async Task<(string[] RecoveryCodes, string Error)> ConfirmSetupAsync(int userId, string code)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (null, "User not found.");

            if (user.IsMfaEnabled)
                return (null, "MFA is already enabled.");

            if (string.IsNullOrWhiteSpace(user.EncryptedTotpSecret))
                return (null, "MFA setup has not been initiated. Call setup first.");

            // Decrypt the secret and verify the code
            var base32Secret = _encryption.Decrypt(user.EncryptedTotpSecret, user.EncryptedDataKey);
            var secretBytes = Base32Encoding.ToBytes(base32Secret);

            var totp = new Totp(secretBytes);
            if (!totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
                return (null, "Invalid verification code. Please try again.");

            // Generate recovery codes
            var recoveryCodes = GenerateRecoveryCodes();
            var recoveryJson = JsonSerializer.Serialize(recoveryCodes);
            user.EncryptedRecoveryCodes = _encryption.Encrypt(recoveryJson, user.EncryptedDataKey);

            // Enable MFA
            user.IsMfaEnabled = true;
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("MFA enabled for user {UserId}.", userId);
            return (recoveryCodes, null);
        }

        // =============================================
        // MFA VERIFICATION (LOGIN)
        // =============================================

        /// <summary>
        /// Verifies a TOTP code during login. Returns true if valid.
        /// </summary>
        public async Task<(bool Valid, string Error)> VerifyCodeAsync(int userId, string code)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (false, "User not found.");

            if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.EncryptedTotpSecret))
                return (false, "MFA is not enabled for this account.");

            var base32Secret = _encryption.Decrypt(user.EncryptedTotpSecret, user.EncryptedDataKey);
            var secretBytes = Base32Encoding.ToBytes(base32Secret);

            var totp = new Totp(secretBytes);
            if (totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1)))
                return (true, null);

            return (false, "Invalid verification code.");
        }

        /// <summary>
        /// Verifies a recovery code during login. Each recovery code can only be used once.
        /// </summary>
        public async Task<(bool Valid, string Error)> VerifyRecoveryCodeAsync(int userId, string recoveryCode)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (false, "User not found.");

            if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.EncryptedRecoveryCodes))
                return (false, "MFA is not enabled or no recovery codes exist.");

            var recoveryJson = _encryption.Decrypt(user.EncryptedRecoveryCodes, user.EncryptedDataKey);
            var codes = JsonSerializer.Deserialize<List<string>>(recoveryJson);

            var normalizedInput = recoveryCode.Replace("-", "").Replace(" ", "").ToUpperInvariant();

            var matchIndex = codes.FindIndex(c =>
                c.Replace("-", "").Replace(" ", "").ToUpperInvariant() == normalizedInput);

            if (matchIndex < 0)
                return (false, "Invalid recovery code.");

            // Remove the used code
            codes.RemoveAt(matchIndex);
            var updatedJson = JsonSerializer.Serialize(codes);
            user.EncryptedRecoveryCodes = _encryption.Encrypt(updatedJson, user.EncryptedDataKey);
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Recovery code used for user {UserId}. {Remaining} codes remaining.", userId, codes.Count);
            return (true, null);
        }

        // =============================================
        // MFA DISABLE
        // =============================================

        /// <summary>
        /// Disables MFA for a user after verifying their password.
        /// Clears the TOTP secret and recovery codes.
        /// </summary>
        public async Task<(bool Success, string Error)> DisableAsync(int userId, string password)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (false, "User not found.");

            if (!user.IsMfaEnabled)
                return (false, "MFA is not currently enabled.");

            // Require password confirmation to disable MFA
            if (!_encryption.VerifyPassword(password, user.PasswordHash))
                return (false, "Invalid password.");

            user.IsMfaEnabled = false;
            user.EncryptedTotpSecret = null;
            user.EncryptedRecoveryCodes = null;
            user.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("MFA disabled for user {UserId}.", userId);
            return (true, null);
        }

        // =============================================
        // STATUS
        // =============================================

        /// <summary>
        /// Returns the MFA status for a user.
        /// </summary>
        public async Task<(bool IsMfaEnabled, int RemainingRecoveryCodes)> GetStatusAsync(int userId)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return (false, 0);

            if (!user.IsMfaEnabled || string.IsNullOrWhiteSpace(user.EncryptedRecoveryCodes))
                return (user.IsMfaEnabled, 0);

            var recoveryJson = _encryption.Decrypt(user.EncryptedRecoveryCodes, user.EncryptedDataKey);
            var codes = JsonSerializer.Deserialize<List<string>>(recoveryJson);

            return (true, codes.Count);
        }

        // =============================================
        // HELPERS
        // =============================================

        private static string[] GenerateRecoveryCodes()
        {
            var codes = new string[RecoveryCodeCount];
            for (int i = 0; i < RecoveryCodeCount; i++)
            {
                // Generate 8-character alphanumeric codes formatted as XXXX-XXXX
                var bytes = RandomNumberGenerator.GetBytes(5);
                var raw = Convert.ToHexString(bytes).ToUpperInvariant()[..8];
                codes[i] = $"{raw[..4]}-{raw[4..]}";
            }
            return codes;
        }
    }
}
