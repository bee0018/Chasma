using ChasmaWebApi.Data.Interfaces;
using System.Security.Cryptography;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class for password hashing and verification.
    /// </summary>
    public class PasswordUtility : IPasswordUtility
    {
        // <inheritdoc/>
        public (string Hash, byte[] Salt) HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return (hash, salt);
        }

        // <inheritdoc/>
        public bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return hash == storedHash;
        }
    }
}
