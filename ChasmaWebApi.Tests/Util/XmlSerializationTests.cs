using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Tests.Util
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
            Assert.IsNotNull(webApiConfigurations);
            Assert.IsFalse(string.IsNullOrEmpty(webApiConfigurations.WebApiUrl));
            Assert.IsFalse(webApiConfigurations.ShowDebugControllers);
            Assert.IsFalse(string.IsNullOrEmpty(webApiConfigurations.ThinClientUrl));
            Assert.IsFalse(string.IsNullOrEmpty(webApiConfigurations.GitHubApiToken));
            Assert.IsFalse(webApiConfigurations.WorkflowRunReportThreshold <= 0);
        }

        /// <summary>
        /// Tests that the <see cref="ChasmaXmlBase.DeserializeToObject{T}(string)"/> method converts XML text into its expected concrete object.
        /// </summary>
        [TestMethod]
        public void TestDeserializationToObject()
        {
            ChasmaWebApiConfigurations config = GetTestConfigurationData();
            string xmlText = ChasmaXmlBase.GenerateXml(config);
            ChasmaWebApiConfigurations? generatedConfig = ChasmaXmlBase.DeserializeToObject<ChasmaWebApiConfigurations>(xmlText);
            Assert.IsNotNull(generatedConfig);
            Assert.AreEqual(config.WebApiUrl, generatedConfig.WebApiUrl);
            Assert.AreEqual(config.GitHubApiToken, generatedConfig.GitHubApiToken);
            Assert.AreEqual(config.ShowDebugControllers, generatedConfig.ShowDebugControllers);
            Assert.AreEqual(config.ThinClientUrl, generatedConfig.ThinClientUrl);
            Assert.AreEqual(config.WorkflowRunReportThreshold, generatedConfig.WorkflowRunReportThreshold);
        }

        /// <summary>
        /// Gets a sample web API configuration object.
        /// </summary>
        /// <returns>A simple web API configuration object.</returns>
        private static ChasmaWebApiConfigurations GetTestConfigurationData()
        {
            return new ChasmaWebApiConfigurations()
            {
                WebApiUrl = "webApiUrl",
                GitHubApiToken = "token",
                ShowDebugControllers = true,
                ThinClientUrl = "thinClientUrl",
                WorkflowRunReportThreshold = 20,
            };
        }
    }
}
