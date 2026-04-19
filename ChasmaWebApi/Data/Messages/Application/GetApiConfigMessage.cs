using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Messages.Application
{
    /// <summary>
    /// Class representing the message to get the API configuration.
    /// </summary>
    public class GetApiConfigMessage : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the Chasma web API url.
        /// </summary>
        public string WebApiUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL of the thin client.
        /// </summary>
        public string ThinClientUrl { get; set; }

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
        /// Gets or sets a value indicating whether the Bitbucket API token is configured.
        /// </summary>
        public string? BitbucketApiToken { get; set; }
    }
}
