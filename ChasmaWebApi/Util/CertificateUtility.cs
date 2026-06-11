using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Utility class responsible for managing local certificates, such as creating self-signed certificates for development purposes and handling certificate storage and retrieval.
    /// </summary>
    public static class CertificateUtility
    {
        /// <summary>
        /// The subject name to use for the self-signed certificate.
        /// </summary>
        private const string CertificateSubject = "CN=localhost.emryce.local";

        /// <summary>
        /// The friendly name to use for the self-signed certificate.
        /// </summary>
        private const string CertificateFriendlyName = "Emryce Local Development Engine";

        /// <summary>
        /// Gets or creates a self-signed certificate.
        /// </summary>
        /// <returns>The self-signed cerficate for HTTPS use.</returns>
        public static X509Certificate2 GetOrCreateLocalCertificate()
        {
            using X509Store store = new(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            foreach (X509Certificate2 certificate in store.Certificates)
            {
                if (certificate.Subject.Equals(CertificateSubject, StringComparison.OrdinalIgnoreCase))
                {
                    // Verify it hasn't expired yet
                    if (DateTime.Now < certificate.NotAfter)
                    {
                        return certificate;
                    }

                    // If expired, clean it up out of the store
                    store.Remove(certificate);
                    break;
                }
            }

            return GenerateTrustedCertificate(store);
        }

        /// <summary>
        /// Generates a new self-signed certificate with the specified subject and adds it to the user's personal certificate store.
        /// </summary>
        /// <param name="userStore">The certificate user store.</param>
        /// <returns>The self-signed certificate.</returns>
        private static X509Certificate2 GenerateTrustedCertificate(X509Store userStore)
        {
            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            X509KeyUsageExtension standardExtension = new(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true);
            request.CertificateExtensions.Add(standardExtension);

            OidCollection oidCollection = [new Oid("1.3.6.1.5.5.7.3.1")];
            X509EnhancedKeyUsageExtension keyUsageExtension = new(oidCollection, true);
            request.CertificateExtensions.Add(keyUsageExtension);

            SubjectAlternativeNameBuilder alternativeNameBuilder = new();
            alternativeNameBuilder.AddDnsName("localhost");
            alternativeNameBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
            request.CertificateExtensions.Add(alternativeNameBuilder.Build());

            // Set short lifespan (e.g., 30 days)
            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
            DateTimeOffset notAfter = notBefore.AddDays(30);
            X509Certificate2 generatedCert = request.CreateSelfSigned(notBefore, notAfter);
            if (OperatingSystem.IsWindows())
            {
                generatedCert.FriendlyName = CertificateFriendlyName;
            }

            byte[] rawCertificateData = generatedCert.Export(X509ContentType.Pfx);
            string certificatePassword = null;
            X509Certificate2 exportableCert = X509CertificateLoader.LoadPkcs12(rawCertificateData, certificatePassword, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet);
            userStore.Add(exportableCert);
            try
            {
                using X509Store rootStore = new(StoreName.Root, StoreLocation.CurrentUser);
                rootStore.Open(OpenFlags.ReadWrite);
                rootStore.Add(exportableCert);
            }
            catch (CryptographicException)
            {
                // Fallback gracefully if the OS security policy restricts programmatically injecting root certificates without administrative prompt.
                // Kestrel will still run, but the browser may show a "Self-Signed" warning.
            }

            return exportableCert;
        }
    }
}
