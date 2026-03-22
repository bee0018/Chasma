using ChasmaWebApi.Data.Objects.Application;
using NGitLab;
using Octokit;

namespace ChasmaWebApi.Util
{
    /// <summary>
    /// Helper class used for aiding in operations with remote hosted platforms.
    /// </summary>
    public static class RemoteHelper
    {
        /// <summary>
        /// Gets the GitHub API client.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <returns>The GitHub API client.</returns>
        public static GitHubClient GetGitHubClient(string repoName, string token)
        {
            ProductHeaderValue header = new(repoName);
            Credentials credentials = new(token);
            return new GitHubClient(header)
            {
                Credentials = credentials,
            };
        }

        /// <summary>
        /// Gets the GitLab API client.
        /// </summary>
        /// <param name="token">The GitLab API access token.</param>
        /// <param name="selfHostedUrl">If provided, the self hosted URL of the GitLab instance.</param>
        /// <returns>The GitLab API client.</returns>
        public static GitLabClient GetGitLabClient(string token, string selfHostedUrl = null)
        {
            if (!string.IsNullOrEmpty(selfHostedUrl))
            {
                return new GitLabClient(selfHostedUrl, token);
            }

            return new GitLabClient("https://gitlab.com", token);
        }

        /// <summary>
        /// Determines the remote host platform of the repository.
        /// </summary>
        /// <param name="remoteUrl">The specified repository's url.</param>
        /// <returns>The remote host platform.</returns>
        public static RemoteHostPlatform GetRemoteHostPlatform(string remoteUrl)
        {
            string url = remoteUrl.ToLower();
            if (url.Contains("github.com"))
            {
                return RemoteHostPlatform.GitHub;
            }
            else if (url.Contains("gitlab.com"))
            {
                return RemoteHostPlatform.GitLab;
            }
            else if (url.Contains("bitbucket.org"))
            {
                return RemoteHostPlatform.Bitbucket;
            }
            else
            {
                return RemoteHostPlatform.Unknown;
            }
        }
    }
}
