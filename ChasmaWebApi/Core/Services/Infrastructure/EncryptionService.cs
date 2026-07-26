using ChasmaWebApi.Core.Interfaces.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text;

namespace ChasmaWebApi.Core.Services.Infrastructure
{
    /// <summary>
    /// Utility class representing a class for encrypting and decrypting data.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        /// <summary>
        /// The data protector for linux operating systems.
        /// </summary>
        private readonly IDataProtector linuxProtector;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionService"/> class.
        /// </summary>
        /// <param name="dataProtectionProvider">The data protection provider.</param>
        public EncryptionService(IDataProtectionProvider dataProtectionProvider)
        {
            if (OperatingSystem.IsLinux())
            {
                linuxProtector = dataProtectionProvider.CreateProtector("Emryce.UserApiKeyProtection.v1");
            }
        }

        #endregion

        // <inheritdoc />
        public string EncryptString(string plainTextCredential)
        {
            if (string.IsNullOrEmpty(plainTextCredential))
            {
                return string.Empty;
            }

            if (OperatingSystem.IsWindows())
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainTextCredential);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }

            if (OperatingSystem.IsLinux())
            {
                return linuxProtector.Protect(plainTextCredential);
            }

            return plainTextCredential;
        }

        // <inheritdoc />
        public string DecryptString(string encryptedCredential)
        {
            if (string.IsNullOrEmpty(encryptedCredential))
            {
                return string.Empty;
            }

            if (OperatingSystem.IsWindows())
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedCredential);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }

            if (OperatingSystem.IsLinux())
            {
                return linuxProtector.Unprotect(encryptedCredential);
            }

            return encryptedCredential;
        }
    }
}
