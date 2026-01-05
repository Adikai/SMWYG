using System;
using System.Security.Cryptography;
using System.Text;

namespace SMWYG.Utils
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return string.Empty;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] combined = new byte[salt.Length + passwordBytes.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, salt.Length, passwordBytes.Length);

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(combined);

            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string storedHash, string candidate)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(candidate))
                return false;

            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                // Fallback for legacy plaintext storage
                return string.Equals(storedHash, candidate, StringComparison.Ordinal);
            }

            try
            {
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] stored = Convert.FromBase64String(parts[1]);
                byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);
                byte[] combined = new byte[salt.Length + candidateBytes.Length];
                Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
                Buffer.BlockCopy(candidateBytes, 0, combined, salt.Length, candidateBytes.Length);

                using var sha256 = SHA256.Create();
                byte[] candidateHash = sha256.ComputeHash(combined);

                return CryptographicOperations.FixedTimeEquals(stored, candidateHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
