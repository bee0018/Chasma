namespace ChasmaWebApi.Data.Interfaces
{
    /// <summary>
    /// Defines the members of the password utility interface.
    /// </summary>
    public interface IPasswordUtility
    {
        /// <summary>
        /// Hash the given password using PBKDF2 with a random salt.
        /// </summary>
        /// <param name="password">The plain text password.</param>
        /// <returns>The hashed password with the salt.</returns>
        (string Hash, byte[] Salt) HashPassword(string password);

        /// <summary>
        /// Verifies if the stored hash matches the hash of the given password and salt.
        /// </summary>
        /// <param name="password">The login password.</param>
        /// <param name="salt">The user account salt.</param>
        /// <param name="storedHash">The database password.</param>
        /// <returns>True if the passwords match; false otherwise.</returns>
        bool VerifyPassword(string password, byte[] salt, string storedHash);
    }
}
