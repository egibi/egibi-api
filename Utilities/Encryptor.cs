#nullable disable
using System.Security.Cryptography;

namespace egibi_api.Utilities
{
    public static class Encryptor
    {
        public static string EncryptString(string unencrypted, string password)
        {
            // Convert the plaintext string to a byte array
            byte[] unencryptedBytes = System.Text.Encoding.UTF8.GetBytes(unencrypted);

            // Derive a new password using the PBKDF2 algorithm and a random salt
            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, 20, 1000, HashAlgorithmName.SHA256);

            // Use the password to encrypt the plaintext
            Aes encryptor = Aes.Create();
            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(unencryptedBytes, 0, unencryptedBytes.Length);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string DecryptString(string encrypted, string password)
        {
            // Convert the encrypted string to a byte array
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);
 
            // Derive the password using the PBKDF2 algorithm
            Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, 20, 1000, HashAlgorithmName.SHA256);
 
            // Use the password to decrypt the encrypted string
            Aes encryptor = Aes.Create();
            encryptor.Key = passwordBytes.GetBytes(32);
            encryptor.IV = passwordBytes.GetBytes(16);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                }
                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }

    }

}
