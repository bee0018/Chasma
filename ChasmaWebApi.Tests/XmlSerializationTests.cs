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
            Assert.AreEqual(databaseConfig.Username, webApiConfigurations.DatabaseConfigurations.Username);
            Assert.AreEqual(databaseConfig.Password, webApiConfigurations.DatabaseConfigurations.Password);
            Assert.AreEqual(databaseConfig.Port, webApiConfigurations.DatabaseConfigurations.Port);
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
            Assert.AreEqual(databaseConfig.Username, generatedConfig.Username);
            Assert.AreEqual(databaseConfig.Password, generatedConfig.Password);
            Assert.AreEqual(databaseConfig.Port, generatedConfig.Port);
        }

        /// <summary>
        /// Gets a sample Chasma database configuration.
        /// </summary>
        /// <returns>The sample database configurations.</returns>
        private DatabaseConfigurations GetTestDatabaseConfigurationData()
        {
            return new DatabaseConfigurations
            {
                DatabaseName = "chasma",
                Host = "localhost",
                Password = "password",
                Port = 5432,
                Username = "postgres",
            };
        }
    }
}
