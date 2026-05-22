using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages.Application
{
    /// <summary>
    /// Class representing the message to get the API configuration.
    /// </summary>
    public class GetApiConfigMessage : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets where the port to listens on all IPs using IPv6 [::], or IPv4 0.0.0.0 if IPv6 is not supported.
        /// </summary>
        public int BindingPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the JWT secret key is configured.
        /// </summary>
        public bool JwtSecretKeyConfigured { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the GitHub API token is configured.
        /// </summary>
        public bool GitHubApiTokenConfigured { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the GitLab API token is configured.
        /// </summary>
        public bool GitLabApiTokenConfigured { get; set; }

        /// <summary>
        /// Gets or sets the workflow run report threshold.
        /// </summary>
        public int? WorkflowRunReportThreshold { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds for scanning GitHub pull requests.
        /// </summary>
        public int? GitHubPullRequestScanIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the self hosted GitLab url.
        /// </summary>
        public string? SelfHostedGitLabUrl { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which GitLab merge requests are scanned for updates.
        /// </summary>
        public int? GitLabMergeRequestScanIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets the user's GitHub username.
        /// </summary>
        public string? GitHubUsername { get; set; }

        /// <summary>
        /// Gets or sets the user's GitLab username.
        /// </summary>
        public string? GitLabUsername { get; set; }

        /// <summary>
        /// Gets or sets the global workspace path.
        /// </summary>
        public string GlobalWorkspacePath { get; set; }

        /// <summary>
        /// Gets or sets the GitHub account SSH key private key file path.
        /// </summary>
        public string? GitHubSshKeyPrivateKeyPath { get; set; }

        /// <summary>
        /// Gets or sets the GitLab account SSH key private key file path.
        /// </summary>
        public string? GitLabSshKeyPrivateKeyPath { get; set; }

        /// <summary>
        /// Gets or sets the GitHub account SSH key pass phrase.
        /// </summary>
        public string? GitHubSshPassphrase { get; set; }

        /// <summary>
        /// Gets or sets the GitLab account SSH key pass phrase.
        /// </summary>
        public string? GitLabSshPassphrase { get; set; }
    }
}
