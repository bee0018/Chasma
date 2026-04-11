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
        public int WorkflowRunReportThreshold { get; set; } = 30;

        /// <summary>
        /// Gets or sets the interval in seconds at which GitHub pull requests are scanned for updates.
        /// </summary>
        [XmlElement("gitHubPullRequestScanIntervalSeconds")]
        public int GitHubPullRequestScanIntervalSeconds { get; set; } = 45;

        /// <summary>
        /// Gets or sets the GitLab API token.
        /// </summary>
        [XmlElement("gitlabApiToken")]
        public string GitLabApiToken { get; set; }

        /// <summary>
        /// Gets or sets the self hosted GitLab url.
        /// </summary>
        [XmlElement("selfHostedGitLabUrl")]
        public string SelfHostedGitLabUrl { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which GitLab merge requests are scanned for updates.
        /// </summary>
        [XmlElement("gitLabMergeRequestScanIntervalSeconds")]
        public int GitLabMergeRequestScanIntervalSeconds { get; set; } = 45;

        /// <summary>
        /// Gets or sets the Bitbucket API token.
        /// </summary>
        [XmlElement("bitbucketApiToken")]
        public string BitbucketApiToken { get; set; }

        /// <summary>
        /// Gets or sets the JWT secret key.
        /// </summary>
        [XmlElement("jwtSecretKey")]
        public string JwtSecretKey { get; set; }

        /// <summary>
        /// Gets or sets where the port to listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
        /// </summary>
        [XmlElement("bindingPort")]
        public int BindingPort { get; set; }

        /// <summary>
        /// Gets the connection string in the format that is expected of SQLite3 database connections.
        /// </summary>
        /// <returns>The formatted database connection string.</returns>
        public string GetDatabaseConnectionString()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Chasma"
            );
            Directory.CreateDirectory(folder);
            string dbPath = Path.Combine(folder, "Chasma.db");
            return $"Data Source={dbPath}";
        }
    }
}
