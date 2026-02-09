#nullable disable
namespace egibi_api.Services.Security
{
    /// <summary>
    /// Provides application-layer encryption for sensitive data.
    /// Uses AES-256-GCM with a two-tier key hierarchy:
    ///   - Master Key (from config/vault) encrypts per-user Data Encryption Keys (DEKs)
    ///   - Per-user DEKs encrypt individual secrets (API keys, passwords, etc.)
    /// </summary>
    public interface IEncryptionService
    {
        // =============================================
        // USER KEY MANAGEMENT
        // =============================================

        /// <summary>
        /// Generates a new random Data Encryption Key for a user.
        /// Returns the DEK encrypted with the master key (safe to store in DB).
        /// Call this once during user account creation.
        /// </summary>
        string GenerateUserKey();

        // =============================================
        // ENCRYPT / DECRYPT OPERATIONS
        // =============================================

        /// <summary>
        /// Encrypts a plaintext secret using the user's encrypted DEK.
        /// The DEK is temporarily decrypted in memory, used, then discarded.
        /// Returns a base64 string containing [nonce + ciphertext + tag].
        /// </summary>
        /// <param name="plaintext">The secret to encrypt (e.g., an API key)</param>
        /// <param name="encryptedUserKey">The user's DEK from the database (encrypted with master key)</param>
        string Encrypt(string plaintext, string encryptedUserKey);

        /// <summary>
        /// Decrypts a secret that was encrypted with Encrypt().
        /// </summary>
        /// <param name="ciphertext">The base64 encrypted string from the database</param>
        /// <param name="encryptedUserKey">The user's DEK from the database (encrypted with master key)</param>
        string Decrypt(string ciphertext, string encryptedUserKey);

        // =============================================
        // PASSWORD HASHING (one-way, for login credentials)
        // =============================================

        /// <summary>
        /// Hashes a password using bcrypt. Used for user login passwords.
        /// This is ONE-WAY — you cannot recover the original password.
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a plaintext password against a bcrypt hash.
        /// </summary>
        bool VerifyPassword(string password, string hash);

        // =============================================
        // KEY ROTATION SUPPORT
        // =============================================

        /// <summary>
        /// Re-encrypts a user's DEK with a new master key.
        /// Used during master key rotation.
        /// </summary>
        /// <param name="encryptedUserKey">DEK encrypted with the OLD master key</param>
        /// <param name="oldMasterKey">The previous master key (base64)</param>
        /// <returns>DEK encrypted with the CURRENT master key</returns>
        string RotateMasterKey(string encryptedUserKey, string oldMasterKey);

        // =============================================
        // MASTER KEY ACCESS (for HMAC signing)
        // =============================================

        /// <summary>
        /// Returns the raw master key bytes for HMAC operations (e.g., MFA challenge tokens).
        /// Use with caution — only for signing/verification, never for direct data storage.
        /// </summary>
        byte[] GetMasterKeyBytes();
    }
}
