using ChasmaWebApi.Core.Interfaces.Infrastructure;
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
            using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return (hash, salt);
        }

        // <inheritdoc/>
        public bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            using Rfc2898DeriveBytes pbkdf2 = new(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return hash == storedHash;
        }

        // <inheritdoc/>
        public bool IsPasswordValid(string password)
        {
            bool hasLower = false;
            bool hasUpper = false;
            bool hasSymbol = false;
            bool hasDigit = false;
            foreach (char character in password)
            {
                if (char.IsLower(character))
                {
                    hasLower = true;
                }
                else if (char.IsUpper(character))
                {
                    hasUpper = true;
                }
                else if (!char.IsLetterOrDigit(character))
                {
                    hasSymbol = true;
                }
                else if (char.IsDigit(character))
                {
                    hasDigit = true;
                }

                if (hasLower && hasUpper && hasSymbol && hasDigit && password.Length >= 10)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
