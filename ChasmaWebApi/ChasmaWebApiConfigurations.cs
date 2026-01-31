using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi
{
    /// <summary>
    /// Class representing the API configuration options.
    /// </summary>
    [XmlRoot("configurations")]
    public class ChasmaWebApiConfigurations : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the Chasma web API url.
        /// </summary>
        [XmlElement("webApiUrl")]
        public required string WebApiUrl { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to show the debug API controllers.
        /// </summary>
        [XmlElement("showDebugControllers")]
        public required bool ShowDebugControllers { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the thin client.
        /// </summary>
        [XmlElement("thinClientUrl")]
        public required string ThinClientUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the GitHub API token.
        /// </summary>
        [XmlElement("githubApiToken")]
        public string GitHubApiToken { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of workflows to report to the client.
        /// </summary>
        [XmlElement("workflowRunReportThreshold")]
        public int WorkflowRunReportThreshold { get; set; }

        /// <summary>
        /// Gets the connection string in the format that is expected of SQLite3 database connections.
        /// </summary>
        /// <returns>The formatted database connection string.</returns>
        public string GetDatabaseConnectionString()
        {
            return $"Data Source=Chasma.db";
        }
    }
}
