namespace ChasmaWebApi.Core.Interfaces.Infrastructure
{
    /// <summary>
    /// The interface defining the encryption service.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts the plain text string to a protected credential.
        /// </summary>
        /// <param name="plainTextCredential">The plain text credential.</param>
        /// <returns>The encrypted credential.</returns>
        string EncryptString(string plainTextCredential);

        /// <summary>
        /// Decrypts the encrypted credential to a plain text string.
        /// </summary>
        /// <param name="encryptedCredential">The encrypted credential.</param>
        /// <returns>The plain text string.</returns>
        string DecryptString(string encryptedCredential);
    }
}
