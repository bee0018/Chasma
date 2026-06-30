using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
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
        public static GitLabClient GetGitLabClient(string token, string? selfHostedUrl = null)
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

        /// <summary>
        /// Gets the remote host platform API token based on the repository type.
        /// </summary>
        /// <param name="remoteHostPlatform">The repository's remote host platform.</param>
        /// <returns>The repository remote host platform API token.</returns>
        public static string GetApiToken(RemoteHostPlatform remoteHostPlatform)
        {
            ChasmaWebApiConfigurations apiConfig = ChasmaWebApiConfigurations.GetApiConfig();
            return remoteHostPlatform switch
            {
                RemoteHostPlatform.GitHub => apiConfig.GitHubApiToken,
                RemoteHostPlatform.GitLab => apiConfig.GitLabApiToken,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// Gets the username of the remote host of where the repository is hosted.
        /// </summary>
        /// <param name="repository">The local git repository.</param>
        /// <returns>The remote host username.</returns>
        public static string GetRemoteHostUsername(LocalGitRepository repository)
        {
            RemoteHostPlatform remoteHostPlatform = repository.HostPlatform;
            ChasmaWebApiConfigurations apiConfig = ChasmaWebApiConfigurations.GetApiConfig();
            return remoteHostPlatform switch
            {
                RemoteHostPlatform.GitHub => apiConfig.GitHubUsername,
                RemoteHostPlatform.GitLab => apiConfig.GitLabUsername,
                _ => string.Empty,
            };
        }
    }
}
