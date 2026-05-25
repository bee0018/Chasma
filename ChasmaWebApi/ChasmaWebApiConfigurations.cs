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
        /// Gets or sets where the port to listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
        /// </summary>
        [XmlElement("bindingPort")]
        public int BindingPort { get; set; }

        /// <summary>
        /// Gets or sets the JWT secret key.
        /// </summary>
        [XmlElement("jwtSecretKey")]
        public string JwtSecretKey { get; set; }

        /// <summary>
        /// Gets or sets the global workspace path where all repositories will be stored.
        /// </summary>
        [XmlElement("globalWorkspacePath")]
        public string GlobalWorkspacePath { get; set; }

        /// <summary>
        /// Gets or sets the user's GitHub username.
        /// </summary>
        [XmlElement("gitHubUsername")]
        public string? GitHubUsername { get; set; }

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
        /// Gets or sets the user's GitLab username.
        /// </summary>
        [XmlElement("gitLabUsername")]
        public string? GitLabUsername { get; set; }

        /// <summary>
        /// Gets or sets the filepath of the generated PEM RSA SSH key file for the GitHub account.
        /// </summary>
        [XmlElement("gitHubSshPrivateKeyPath")]
        public string? GitHubSshKeyPrivateKeyPath { get; set; }

        /// <summary>
        /// Gets or sets the user-defineed password used to encrypt an SSH private key file when it is generated for a GitHub account.
        /// </summary>
        [XmlElement("gitHubSshPassphrase")]
        public string? GitHubSshPassphrase { get; set; }

        /// <summary>
        /// Gets or sets the GitLab API token.
        /// </summary>
        [XmlElement("gitlabApiToken")]
        public string? GitLabApiToken { get; set; }

        /// <summary>
        /// Gets or sets the filepath of the generated PEM RSA SSH key file for the GitLab account.
        /// </summary>
        [XmlElement("gitLabSshPrivateKeyPath")]
        public string? GitLabSshKeyPrivateKeyPath { get; set; }

        /// <summary>
        /// Gets or sets the user-defineed password used to encrypt an SSH private key file when it is generated for a GitLab account.
        /// </summary>
        [XmlElement("gitLabSshPassphrase")]
        public string? GitLabSshPassphrase { get; set; }

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

        #endregion

        /// <summary>
        /// The default JWT secret key.
        /// </summary>
        public const string DefaultJwtSecretKey = "TEMP_DEV_KEY_1234567890";

        /// <summary>
        /// Gets or sets a value indicating whether the application is running in development mode.
        /// </summary>
        public static bool IsDevelopmentMode { get; set; }

        /// <summary>
        /// Gets the connection string in the format that is expected of SQLite3 database connections.
        /// </summary>
        /// <returns>The formatted database connection string.</returns>
        public string GetDatabaseConnectionString()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Emryce"
            );
            Directory.CreateDirectory(folder);
            string dbPath = Path.Combine(folder, "Emryce.db");
            return $"Data Source={dbPath}";
        }

        /// <summary>
        /// Updates the current configuration with values from a new configuration object. Only properties that have different values will be updated.
        /// </summary>
        /// <param name="newConfig">The incoming configuration.</param>
        public void Update(ChasmaWebApiConfigurations newConfig)
        {
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

            if (JwtSecretKey != newConfig.JwtSecretKey)
            {
                JwtSecretKey = newConfig.JwtSecretKey;
            }

            if (BindingPort != newConfig.BindingPort)
            {
                BindingPort = newConfig.BindingPort;
            }

            if (GitHubUsername != newConfig.GitHubUsername)
            {
                GitHubUsername = newConfig.GitHubUsername;
            }

            if (GitLabUsername != newConfig.GitLabUsername)
            {
                GitLabUsername = newConfig.GitLabUsername;
            }

            if (GlobalWorkspacePath != newConfig.GlobalWorkspacePath)
            {
                GlobalWorkspacePath = newConfig.GlobalWorkspacePath;
            }

            if (GitHubSshKeyPrivateKeyPath != newConfig.GitHubSshKeyPrivateKeyPath)
            {
                GitHubSshKeyPrivateKeyPath = newConfig.GitHubSshKeyPrivateKeyPath;
            }

            if (GitHubSshPassphrase != newConfig.GitHubSshPassphrase)
            {
                GitHubSshPassphrase = newConfig.GitHubSshPassphrase;
            }

            if (GitLabSshKeyPrivateKeyPath != newConfig.GitLabSshKeyPrivateKeyPath)
            {
                GitLabSshKeyPrivateKeyPath = newConfig.GitLabSshKeyPrivateKeyPath;
            }

            if (GitLabSshPassphrase != newConfig.GitLabSshPassphrase)
            {
                GitLabSshPassphrase = newConfig.GitLabSshPassphrase;
            }
        }
        
        /// <summary>
        /// Gets the application's configuration file path.
        /// </summary>
        /// <returns>The configuration file path.</returns>
        public static string GetConfigXmlFilePath()
        {
            string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Emryce");
            string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
            return IsDevelopmentMode
                ? defaultConfigPath
                : Path.Combine(appDataDirectory, "config.xml");
        }

        /// <summary>
        /// Gets the API configuration by deserializing it from the XML file.
        /// </summary>
        /// <returns>The current configuration file.</returns>
        public static ChasmaWebApiConfigurations GetApiConfig()
        {
            string configFilePath = GetConfigXmlFilePath();
            ChasmaWebApiConfigurations apiConfig = DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath);
            return apiConfig;
        }
    }
}
