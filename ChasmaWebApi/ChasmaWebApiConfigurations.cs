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
        #region XML Elements

        /// <summary>
        /// Gets or sets the Chasma web API url.
        /// </summary>
        [XmlElement("webApiUrl")]
        public string WebApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL of the thin client.
        /// </summary>
        [XmlElement("thinClientUrl")]
        public string ThinClientUrl { get; set; }

        /// <summary>
        /// Gets or sets the GitHub API token.
        /// </summary>
        [XmlElement("githubApiToken")]
        public string? GitHubApiToken { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of workflows to report to the client.
        /// </summary>
        [XmlElement("workflowRunReportThreshold")]
        public int? WorkflowRunReportThreshold { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which GitHub pull requests are scanned for updates.
        /// </summary>
        [XmlElement("gitHubPullRequestScanIntervalSeconds")]
        public int? GitHubPullRequestScanIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the GitLab API token.
        /// </summary>
        [XmlElement("gitlabApiToken")]
        public string? GitLabApiToken { get; set; }

        /// <summary>
        /// Gets or sets the self hosted GitLab url.
        /// </summary>
        [XmlElement("selfHostedGitLabUrl")]
        public string? SelfHostedGitLabUrl { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which GitLab merge requests are scanned for updates.
        /// </summary>
        [XmlElement("gitLabMergeRequestScanIntervalSeconds")]
        public int? GitLabMergeRequestScanIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the Bitbucket API token.
        /// </summary>
        [XmlElement("bitbucketApiToken")]
        public string? BitbucketApiToken { get; set; }

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

        #endregion

        /// <summary>
        /// The default JWT secret key.
        /// </summary>
        public const string DefaultJwtSecretKey = "TEMP_DEV_KEY_1234567890";

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

        /// <summary>
        /// Updates the current configuration with values from a new configuration object. Only properties that have different values will be updated.
        /// </summary>
        /// <param name="newConfig">The incoming configuration.</param>
        public void Update(ChasmaWebApiConfigurations newConfig)
        {
            if (WebApiUrl != newConfig.WebApiUrl)
            {
                WebApiUrl = newConfig.WebApiUrl;
            }

            if (ThinClientUrl != newConfig.ThinClientUrl)
            {
                ThinClientUrl = newConfig.ThinClientUrl;
            }

            if (GitHubApiToken != newConfig.GitHubApiToken)
            {
                GitHubApiToken = newConfig.GitHubApiToken;
            }

            if (WorkflowRunReportThreshold != newConfig.WorkflowRunReportThreshold)
            {
                WorkflowRunReportThreshold = newConfig.WorkflowRunReportThreshold;
            }

            if (GitHubPullRequestScanIntervalSeconds != newConfig.GitHubPullRequestScanIntervalSeconds)
            {
                GitHubPullRequestScanIntervalSeconds = newConfig.GitHubPullRequestScanIntervalSeconds;
            }

            if (GitLabApiToken != newConfig.GitLabApiToken)
            {
                GitLabApiToken = newConfig.GitLabApiToken;
            }

            if (SelfHostedGitLabUrl != newConfig.SelfHostedGitLabUrl)
            {
                SelfHostedGitLabUrl = newConfig.SelfHostedGitLabUrl;
            }

            if (GitLabMergeRequestScanIntervalSeconds != newConfig.GitLabMergeRequestScanIntervalSeconds)
            {
                GitLabMergeRequestScanIntervalSeconds = newConfig.GitLabMergeRequestScanIntervalSeconds;
            }

            if (BitbucketApiToken != newConfig.BitbucketApiToken)
            {
                BitbucketApiToken = newConfig.BitbucketApiToken;
            }

            if (JwtSecretKey != newConfig.JwtSecretKey)
            {
                JwtSecretKey = newConfig.JwtSecretKey;
            }

            if (BindingPort != newConfig.BindingPort)
            {
                BindingPort = newConfig.BindingPort;
            }
        }
        
        /// <summary>
        /// Gets the application's configuration file path.
        /// </summary>
        /// <param name="isDevelopment">Flag indicating whether the application is in development mode.</param>
        /// <returns>The configuration file path.</returns>
        public static string GetConfigXmlFilePath(bool isDevelopment)
        {
            string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Chasma");
            string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
            return isDevelopment
                ? defaultConfigPath
                : Path.Combine(appDataDirectory, "config.xml");
        }
    }
}
