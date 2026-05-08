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
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            string hash = Convert.ToBase64String(derivedKey);
            return (hash, salt);
        }

        // <inheritdoc/>
        public bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            string hash = Convert.ToBase64String(derivedKey);
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
