using System.Security.Cryptography;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class for password hashing and verification.
    /// </summary>
    public static class PasswordUtility
    {
        /// <summary>
        /// Hash the given password using PBKDF2 with a random salt.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <returns>The hashed password with the salt.</returns>
        public static (string Hash, byte[] Salt) HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return (hash, salt);
        }

        /// <summary>
        /// Verifies if the stored hash matches the hash of the given password and salt.
        /// </summary>
        /// <param name="password">The login password.</param>
        /// <param name="salt">The user account salt.</param>
        /// <param name="storedHash">The database password.</param>
        /// <returns>True if the passwords match; false otherwise.</returns>
        public static bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return hash == storedHash;
        }
    }

}
