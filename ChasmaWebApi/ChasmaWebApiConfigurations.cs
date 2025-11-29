using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Util;
using System.Xml.Serialization;

namespace ChasmaWebApi
{
    [XmlRoot("configurations")]
    public class ChasmaWebApiConfigurations : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the Chasma web API url.
        /// </summary>
        [XmlElement("webApiUrl")]
        public required string WebApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the database configuration options.
        /// </summary>
        [XmlElement("databaseConfiguration")]
        public required DatabaseConfigurations DatabaseConfigurations { get; set; }
        
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
        public required string GitHubApiToken { get; set; }
        
        /// <summary>
        /// Gets or sets the GitHub repository owner.
        /// </summary>
        [XmlElement("githubRepoOwner")]
        public required string GitHubRepoOwner { get; set; }
        
        /// <summary>
        /// Gets or sets the GitHub repository name.
        /// </summary>
        [XmlElement("gitHubRepoName")]
        public required string GitHubRepoName { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum number of workflows to report to the client.
        /// </summary>
        [XmlElement("workflowRunReportThreshold")]
        public required int WorkflowRunReportThreshold { get; set; }
    }
}