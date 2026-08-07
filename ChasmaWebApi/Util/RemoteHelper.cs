using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Services.Infrastructure;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using LibGit2Sharp;
using NGitLab;
using Octokit;
using Repository = LibGit2Sharp.Repository;

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
            Octokit.Credentials credentials = new(token);
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
            if (string.IsNullOrEmpty(remoteUrl))
            {
                return RemoteHostPlatform.Unknown;
            }

            string normalizedRemoteUrl = remoteUrl.Trim().ToLowerInvariant();
            RemoteHostPlatform cloudPlatform = normalizedRemoteUrl switch
            {
                string url when url.Contains("github.com") => RemoteHostPlatform.GitHub,
                string url when url.Contains("gitlab.com") => RemoteHostPlatform.GitLab,
                string url when url.Contains("bitbucket.org") => RemoteHostPlatform.Bitbucket,
                string url when url.Contains("azure.com") || url.Contains("dev.azure.com") || url.Contains("visualstudio.com") => RemoteHostPlatform.AzureDevOps,
                string url when url.Contains("amazonaws.com") => RemoteHostPlatform.AWSCodeCommit,
                string url when url.Contains("launchpad.net") => RemoteHostPlatform.LaunchPad,
                _ => RemoteHostPlatform.Unknown
            };

            if (cloudPlatform != RemoteHostPlatform.Unknown)
            {
                return cloudPlatform;
            }

            bool isValidUrl = Uri.TryCreate(remoteUrl, UriKind.Absolute, out Uri uri) 
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "git" || uri.Scheme == "ssh");
            bool isValidSsh = normalizedRemoteUrl.StartsWith("git@") && normalizedRemoteUrl.Contains(':');
            return (isValidUrl || isValidSsh)
                ? RemoteHostPlatform.Custom
                : RemoteHostPlatform.Unknown;
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

        /// <summary>
        /// Fetches the latest changes from the remote repository for the specified branch, using the provided API token and username for authentication if available.
        /// </summary>
        /// <param name="workingDirectory">The working repository.</param>
        /// <param name="branch">The branch to fetch changes for.</param>
        /// <param name="localRepository">The cached local git repository.</param>
        /// <param name="logger">The logging instance.</param>
        /// <param name="token">The remote host API token.</param>
        public static void FetchLatestChanges(string workingDirectory, LibGit2Sharp.Branch branch, LocalGitRepository localRepository, ILogger logger, string token)
        {
            string username = GetRemoteHostUsername(localRepository);
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
            {
                if (!ShellUtility.TryExecuteShellCommand("git fetch", workingDirectory, out string errorMessage))
                {
                    logger.LogError("Could not execute fetch command because: {error}", errorMessage);
                }
            }
            else
            {
                try
                {
                    using Repository repo = new(workingDirectory);
                    FetchOptions fetchOptions = new()
                    {
                        CredentialsProvider = (url, user, credentials) =>
                        new UsernamePasswordCredentials
                        {
                            Username = username,
                            Password = token
                        }
                    };
                    Commands.Fetch(repo, branch?.RemoteName, [], fetchOptions, null);
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Failed to fetch updates from remote {remote} for repository at {path}. Attempting manual fetch.", branch.RemoteName, workingDirectory);
                    if (!ShellUtility.TryExecuteShellCommand("git fetch", workingDirectory, out string errorMessage))
                    {
                        logger.LogError(errorMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the fetch options for the specified local git repository, using the provided encryption service to decrypt the API token for authentication.
        /// </summary>
        /// <param name="repository">The local git repository.</param>
        /// <param name="encryptionService">The encryption service.</param>
        /// <returns>The fetch options.</returns>
        public static FetchOptions GetFetchOptions(LocalGitRepository repository, IEncryptionService encryptionService)
        {
            string token = GetApiToken(repository.HostPlatform);
            string decryptedToken = encryptionService.DecryptString(token);
            string username = GetRemoteHostUsername(repository);
            return new FetchOptions
            {
                CredentialsProvider = (url, user, credentials) =>
                    new UsernamePasswordCredentials
                    {
                        Username = username,
                        Password = decryptedToken
                    }
            };
        }

        /// <summary>
        /// Gets the push options for the specified local git repository, using the provided encryption service to decrypt the API token for authentication.
        /// </summary>
        /// <param name="repository">The local git repository.</param>
        /// <param name="encryptionService">The encryption service.</param>
        /// <returns>The push options.</returns>
        public static PushOptions GetPushOptions(LocalGitRepository repository, IEncryptionService encryptionService)
        {
            string token = GetApiToken(repository.HostPlatform);
            string decryptedToken = encryptionService.DecryptString(token);
            string username = GetRemoteHostUsername(repository);
            return new PushOptions
            {
                CredentialsProvider = (url, user, credentials) =>
                    new UsernamePasswordCredentials
                    {
                        Username = username,
                        Password = decryptedToken
                    }
            };
        }

        /// <summary>
        /// Gets the pull options for the specified local git repository, using the provided encryption service to decrypt the API token for authentication.
        /// </summary>
        /// <param name="repository">The local git repository.</param>
        /// <param name="encryptionService">The encryption service.</param>
        /// <returns>The pull options.</returns>
        public static PullOptions GetPullOptions(LocalGitRepository repository, IEncryptionService encryptionService)
        {
            FetchOptions fetchOptions = GetFetchOptions(repository, encryptionService);
            return new PullOptions { FetchOptions = fetchOptions };
        }

        /// <summary>
        /// Gets the repository owner from the repository push URL.
        /// </summary>
        /// <param name="pushUrl">The push URL.</param>
        /// <returns>The repository owner.</returns>
        public static string GetRepositoryOwner(string pushUrl)
        {
            string repositoryOwner;
            if (pushUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // HTTPS: https://github.com/OWNER/REPO.git
                Uri pushUri = new(pushUrl);
                string[] httpParts = pushUri.AbsolutePath.Trim('/').Split('/');
                repositoryOwner = httpParts[0];
            }
            else
            {
                // SSH: git@github.com:OWNER/REPO.git
                string path = pushUrl.Split(':')[1];
                string[] sshParts = path.Split('/');
                repositoryOwner = sshParts[0];
            }

            return repositoryOwner;
        }
    }
}
