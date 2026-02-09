#nullable disable
using System.Security.Cryptography;
using System.Text;

namespace egibi_api.Services.Security
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _masterKey;

        // AES-256-GCM constants
        private const int KeySizeBytes = 32;   // 256 bits
        private const int NonceSizeBytes = 12;  // 96 bits (GCM standard)
        private const int TagSizeBytes = 16;    // 128 bits (GCM standard)

        /// <summary>
        /// Initialize with master key from configuration.
        /// The master key should be a base64-encoded 32-byte key.
        /// </summary>
        public EncryptionService(string masterKeyBase64)
        {
            if (string.IsNullOrWhiteSpace(masterKeyBase64))
                throw new ArgumentException("Master encryption key is not configured. " +
                    "Set 'Encryption:MasterKey' in environment variables or a secure vault.");

            _masterKey = Convert.FromBase64String(masterKeyBase64);

            if (_masterKey.Length != KeySizeBytes)
                throw new ArgumentException(
                    $"Master key must be exactly {KeySizeBytes} bytes (256 bits). " +
                    $"Got {_masterKey.Length} bytes. Generate one with: EncryptionService.GenerateMasterKey()");
        }

        // =============================================
        // USER KEY MANAGEMENT
        // =============================================

        /// <inheritdoc />
        public string GenerateUserKey()
        {
            // Generate a random 256-bit DEK
            byte[] dek = RandomNumberGenerator.GetBytes(KeySizeBytes);

            // Encrypt the DEK with the master key
            string encryptedDek = EncryptBytes(dek, _masterKey);

            // Clear the plaintext DEK from memory
            CryptographicOperations.ZeroMemory(dek);

            return encryptedDek;
        }

        // =============================================
        // ENCRYPT / DECRYPT
        // =============================================

        /// <inheritdoc />
        public string Encrypt(string plaintext, string encryptedUserKey)
        {
            if (string.IsNullOrEmpty(plaintext))
                return null;

            // Decrypt the user's DEK using the master key
            byte[] userKey = DecryptToBytes(encryptedUserKey, _masterKey);

            try
            {
                // Encrypt the plaintext with the user's DEK
                byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                string result = EncryptBytes(plaintextBytes, userKey);
                return result;
            }
            finally
            {
                // Always clear the DEK from memory
                CryptographicOperations.ZeroMemory(userKey);
            }
        }

        /// <inheritdoc />
        public string Decrypt(string ciphertext, string encryptedUserKey)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return null;

            // Decrypt the user's DEK using the master key
            byte[] userKey = DecryptToBytes(ciphertext: encryptedUserKey, key: _masterKey);

            try
            {
                // Decrypt the data with the user's DEK
                byte[] plaintextBytes = DecryptToBytes(ciphertext, userKey);
                string result = Encoding.UTF8.GetString(plaintextBytes);

                CryptographicOperations.ZeroMemory(plaintextBytes);
                return result;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(userKey);
            }
        }

        // =============================================
        // PASSWORD HASHING
        // =============================================

        /// <inheritdoc />
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        /// <inheritdoc />
        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        // =============================================
        // KEY ROTATION
        // =============================================

        /// <inheritdoc />
        public string RotateMasterKey(string encryptedUserKey, string oldMasterKey)
        {
            byte[] oldKey = Convert.FromBase64String(oldMasterKey);

            try
            {
                // Decrypt DEK with old master key
                byte[] dek = DecryptToBytes(encryptedUserKey, oldKey);

                try
                {
                    // Re-encrypt DEK with current master key
                    return EncryptBytes(dek, _masterKey);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(dek);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(oldKey);
            }
        }

        // =============================================
        // MASTER KEY ACCESS
        // =============================================

        /// <inheritdoc />
        public byte[] GetMasterKeyBytes()
        {
            // Return a copy to prevent external code from modifying the original
            var copy = new byte[_masterKey.Length];
            Buffer.BlockCopy(_masterKey, 0, copy, 0, _masterKey.Length);
            return copy;
        }

        // =============================================
        // STATIC HELPERS
        // =============================================

        /// <summary>
        /// Generates a new random master key. 
        /// Run this once, store the result securely (env var, vault, etc.).
        /// NEVER store in source control or appsettings.json.
        /// </summary>
        public static string GenerateMasterKey()
        {
            byte[] key = RandomNumberGenerator.GetBytes(KeySizeBytes);
            return Convert.ToBase64String(key);
        }

        // =============================================
        // INTERNAL CRYPTO OPERATIONS (AES-256-GCM)
        // =============================================

        /// <summary>
        /// Encrypts raw bytes with AES-256-GCM.
        /// Output format: base64( nonce[12] + ciphertext[N] + tag[16] )
        /// </summary>
        private static string EncryptBytes(byte[] plaintext, byte[] key)
        {
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSizeBytes];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            // Pack: [nonce][ciphertext][tag]
            byte[] result = new byte[NonceSizeBytes + ciphertext.Length + TagSizeBytes];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, NonceSizeBytes + ciphertext.Length, TagSizeBytes);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a base64 AES-256-GCM payload back to raw bytes.
        /// </summary>
        private static byte[] DecryptToBytes(string ciphertext, byte[] key)
        {
            byte[] combined = Convert.FromBase64String(ciphertext);

            if (combined.Length < NonceSizeBytes + TagSizeBytes)
                throw new CryptographicException("Encrypted data is too short to be valid.");

            int ciphertextLength = combined.Length - NonceSizeBytes - TagSizeBytes;

            byte[] nonce = new byte[NonceSizeBytes];
            byte[] encryptedBytes = new byte[ciphertextLength];
            byte[] tag = new byte[TagSizeBytes];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(combined, NonceSizeBytes, encryptedBytes, 0, ciphertextLength);
            Buffer.BlockCopy(combined, NonceSizeBytes + ciphertextLength, tag, 0, TagSizeBytes);

            byte[] plaintext = new byte[ciphertextLength];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, encryptedBytes, tag, plaintext);

            return plaintext;
        }
    }
}
