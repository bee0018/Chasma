using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Tests
{
    /// <summary>
    /// Class testing the functionality of the <see cref="ChasmaXmlBase"/> class.
    /// </summary>
    [TestClass]
    public class XmlSerializationTests
    {
        /// <summary>
        /// Tests that the <see cref="ChasmaXmlBase.DeserializeFromFile{T}(string)"/> method converts XML into its expected concrete object.
        /// </summary>
        [TestMethod]
        public void TestDeserializationFromFile()
        {
            string configFilePath = "config.xml";
            ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath);
            DatabaseConfigurations databaseConfig = GetTestDatabaseConfigurationData();
            Assert.IsNotNull(webApiConfigurations);
            Assert.IsFalse(string.IsNullOrEmpty(webApiConfigurations.WebApiUrl));
            Assert.AreEqual(databaseConfig.DatabaseName, webApiConfigurations.DatabaseConfigurations.DatabaseName);
            Assert.AreEqual(databaseConfig.Server, webApiConfigurations.DatabaseConfigurations.Server);
            Assert.AreEqual(databaseConfig.TrustedConnection, webApiConfigurations.DatabaseConfigurations.TrustedConnection);
            Assert.AreEqual(databaseConfig.TrustedCertificate, webApiConfigurations.DatabaseConfigurations.TrustedCertificate);
        }

        /// <summary>
        /// Tests that the <see cref="ChasmaXmlBase.DeserializeToObject{T}(string)"/> method converts XML text into its expected concrete object.
        /// </summary>
        [TestMethod]
        public void TestDeserializationToObject()
        {
            DatabaseConfigurations databaseConfig = GetTestDatabaseConfigurationData();
            string xmlText = ChasmaXmlBase.GenerateXml(databaseConfig);
            DatabaseConfigurations? generatedConfig = ChasmaXmlBase.DeserializeToObject<DatabaseConfigurations>(xmlText);
            Assert.IsNotNull(generatedConfig);
            Assert.AreEqual(databaseConfig.DatabaseName, generatedConfig.DatabaseName);
            Assert.AreEqual(databaseConfig.Server, generatedConfig.Server);
            Assert.AreEqual(databaseConfig.TrustedConnection, generatedConfig.TrustedConnection);
            Assert.AreEqual(databaseConfig.TrustedCertificate, generatedConfig.TrustedCertificate);
        }

        /// <summary>
        /// Gets a sample Chasma database configuration.
        /// </summary>
        /// <returns>The sample database configurations.</returns>
        private DatabaseConfigurations GetTestDatabaseConfigurationData()
        {
            return new DatabaseConfigurations
            {
                DatabaseName = "Chasma",
                Server = "localhost\\SQLEXPRESS",
                TrustedConnection = true,
                TrustedCertificate = true,
            };
        }
    }
}
