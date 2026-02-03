using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using Microsoft.EntityFrameworkCore;
using Octokit;

namespace ChasmaWebApi.HostedServices
{
    /// <summary>
    /// Class representing a hosted service that initializes the cache with database information upon application startup.
    /// </summary>
    /// <param name="logger">The internal logging interface.</param>
    /// <param name="cacheManager">The application's cache manager.</param>
    /// <param name="serviceScopeFactory">The service scope factory used for getting required services.</param>
    /// <param name="config">The application configurations.</param>
    public class CacheInitializerService(ILogger<CacheInitializerService> logger, ICacheManager cacheManager, IServiceScopeFactory serviceScopeFactory, ChasmaWebApiConfigurations config) : IHostedService
    {
        /// <summary>
        /// The internal logging interface.
        /// </summary>
        private readonly ILogger<CacheInitializerService> logger = logger;

        /// <summary>
        /// The application's cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager = cacheManager;

        /// <summary>
        /// The service scope factory used for getting required services.
        /// </summary>
        private readonly IServiceScopeFactory serviceScopeFactory = serviceScopeFactory;

        /// <summary>
        /// The web API application configurations.
        /// </summary>
        private readonly ChasmaWebApiConfigurations configurations = config;

        /// <summary>
        /// The GitHub API client.
        /// </summary>
        private GitHubClient Client { get; set; }

        /// <summary>
        /// The timer used to poll for GitHub pull request updates.
        /// </summary>
        private PeriodicTimer PullRequestPollTimer { get; set; }

        // <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => InitializeInternalCacheAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        // <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping the CacheInitializer hosted service and clearing cache.");
            cacheManager.WorkingDirectories.Clear();
            cacheManager.Repositories.Clear();
            cacheManager.Users.Clear();
            cacheManager.GitHubPullRequests.Clear();
            PullRequestPollTimer?.Dispose();
            logger.LogInformation("Cache cleared successfully.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the internal API cache with data stored in the database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        private async Task InitializeInternalCacheAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Initializing the cache with the database information.");
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ApplicationDbContext applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await applicationDbContext.Database.MigrateAsync(cancellationToken);
            List<RepositoryModel> repositories = await applicationDbContext.Repositories.ToListAsync(cancellationToken);
            foreach (RepositoryModel repoModel in repositories)
            {
                LocalGitRepository repository = new()
                {
                    Id = repoModel.Id,
                    UserId = repoModel.UserId,
                    Name = repoModel.Name,
                    Owner = repoModel.Owner,
                    Url = repoModel.Url,
                    IsIgnored = repoModel.IsIgnored,
                };
                cacheManager.Repositories.TryAdd(repository.Id, repository);
            }

            List<WorkingDirectoryModel> workingDirectories = await applicationDbContext.WorkingDirectories.ToListAsync(cancellationToken);
            foreach (WorkingDirectoryModel workingDirectoryModel in workingDirectories)
            {
                // Clean up entries that are no longer valid. User may have deleted repositories outside of the application.
                if (!Directory.Exists(workingDirectoryModel.WorkingDirectory))
                {
                    logger.LogWarning("Working directory {workingDirectory} does not exist anymore. Skipping adding it to the cache and removing from database.", workingDirectoryModel.WorkingDirectory);
                    cacheManager.Repositories.TryRemove(workingDirectoryModel.RepositoryId, out _);
                    RepositoryModel? repository = await applicationDbContext.Repositories.FirstOrDefaultAsync(i => i.Id == workingDirectoryModel.RepositoryId, cancellationToken);
                    if (repository != null)
                    {
                        applicationDbContext.Repositories.Remove(repository);
                    }
                    applicationDbContext.WorkingDirectories.Remove(workingDirectoryModel);
                    applicationDbContext.SaveChanges();
                    continue;
                }

                cacheManager.WorkingDirectories.TryAdd(workingDirectoryModel.RepositoryId, workingDirectoryModel.WorkingDirectory);
            }

            List<UserAccountModel> users = await applicationDbContext.UserAccounts.ToListAsync(cancellationToken);
            foreach (UserAccountModel user in users)
            {
                cacheManager.Users.TryAdd(user.Id, user);
            }

            logger.LogInformation("Finished updating the cache with the database data.");
            _ = Task.Run(() => InitializeNetworkCacheAsync(cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Initializes the network cache with the Git client information.
        /// </summary>
        /// <returns>Task that initializes the cache with data from network clients.</returns>
        private async Task InitializeNetworkCacheAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Populating the network cache with data from Git clients.");
            if (string.IsNullOrEmpty(configurations.GitHubApiToken))
            {
                logger.LogWarning("GitHub API token is not provided. Skipping GitHub network cache initialization.");
                return;
            }

            try
            {
                foreach (LocalGitRepository repository in cacheManager.Repositories.Values)
                {
                    string repoName = repository.Name;
                    Client = CreateGitHubClient(repoName);
                    List<GitHubPullRequest>? pullRequests = await GetPullRequestAsync(Client, repository.Owner, repository.Name);
                    if (pullRequests == null)
                    {
                        logger.LogWarning("Skipping cache initialization for {name}.", repoName);
                        continue;
                    }

                    pullRequests.ForEach(pr =>
                    {
                        cacheManager.GitHubPullRequests.TryAdd(pr.Number, pr);
                    });
                }

                // Start polling ONLY after GitHub pull requests are populated
                StartPullRequestPolling(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError("Error populating network cache: {error}", e);
            }
        }

        #region GitHub

        /// <summary>
        /// Creates a new GitHub API client for the specified repository.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <returns>The GitHub client for the specified repo.</returns>
        private GitHubClient CreateGitHubClient(string repoName)
        {
            return new GitHubClient(new ProductHeaderValue(repoName))
            {
                Credentials = new Credentials(configurations.GitHubApiToken)
            };
        }

        /// <summary>
        /// Gets all the open pull requests via the GitHub API.
        /// </summary>
        /// <param name="client">The Ocktokit GitHub API client.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository owner.</param>
        /// <returns>Task containing the result of the API operation.</returns>
        private async Task<List<GitHubPullRequest>?> GetPullRequestAsync(GitHubClient client, string owner, string repoName)
        {
            try
            {
                IReadOnlyList<PullRequest> pullRequests = await client.PullRequest.GetAllForRepository(owner, repoName);
                List<GitHubPullRequest> gitHubPullRequests = [];
                foreach (PullRequest pullRequest in pullRequests)
                {
                    GitHubPullRequest pr = new()
                    {
                        Number = pullRequest.Number,
                        RepositoryName = pullRequest.Head.Repository.Name,
                        RepositoryOwner = pullRequest.Head.Repository.Owner.Login,
                        BranchName = pullRequest.Head.Ref,
                        ActiveState = pullRequest.State.StringValue,
                        MergeableState = pullRequest.MergeableState.HasValue ? pullRequest.MergeableState.Value.StringValue : MergeableState.Unknown.ToString(),
                        CreatedAt = pullRequest.CreatedAt.ToLocalTime().ToString("g"),
                        MergedAt = pullRequest.MergedAt.HasValue ? pullRequest.MergedAt.Value.ToLocalTime().ToString("g") : null,
                        Merged = pullRequest.Merged,
                        HtmlUrl = pullRequest.HtmlUrl
                    };

                    gitHubPullRequests.Add(pr);
                }
                
                return gitHubPullRequests;
            }
            catch (Exception e)
            {
                logger.LogWarning("Error when trying to get list of open pull request in {repoName}: {error}", repoName, e);
                return null;
            }
        }

        /// <summary>
        /// Gets the pull request by its number via the GitHub API.
        /// </summary>
        /// <param name="client">The GitHub API client.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="name">The repoository name.</param>
        /// <param name="prNumber">The GitHub pull request number.</param>
        /// <returns>The internal GitHub pull request.</returns>
        private async Task<GitHubPullRequest?> GetPullRequestByPrNumberAsync(GitHubClient client, string owner, string name, int prNumber)
        {
            try
            {
                PullRequest? pullRequest = await client.PullRequest.Get(owner, name, prNumber);
                if (pullRequest == null)
                {
                    return null;
                }

                GitHubPullRequest pr = new()
                {
                    Number = pullRequest.Number,
                    BranchName = pullRequest.Head.Ref,
                    RepositoryName = pullRequest.Head.Repository.Name,
                    RepositoryOwner = pullRequest.Head.Repository.Owner.Login,
                    ActiveState = pullRequest.State.StringValue,
                    MergeableState = pullRequest.MergeableState.HasValue ? pullRequest.MergeableState.Value.StringValue : MergeableState.Unknown.ToString(),
                    CreatedAt = pullRequest.CreatedAt.ToLocalTime().ToString("g"),
                    MergedAt = pullRequest.MergedAt.HasValue ? pullRequest.MergedAt.Value.ToLocalTime().ToString("g") : null,
                    Merged = pullRequest.Merged,
                    HtmlUrl = pullRequest.HtmlUrl
                };
                return pr;
            }
            catch (Exception e)
            {
                logger.LogWarning("Error when trying to get pull request #{prNumber}: {error}", prNumber, e);
                return null;
            }
        }

        /// <summary>
        /// Starts the periodic polling of GitHub pull requests.
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        private void StartPullRequestPolling(CancellationToken cancellationToken)
        {
            int intervalSeconds = configurations.GitHubPullRequestScanIntervalSeconds;
            PullRequestPollTimer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
            _ = Task.Run(async () =>
            {
                while (await PullRequestPollTimer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        await RefreshPullRequestsAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error while polling GitHub pull requests: {error}", ex);
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Refreshes the GitHub pull request cache.
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        private async Task RefreshPullRequestsAsync(CancellationToken cancellationToken)
        {
            foreach (GitHubPullRequest existingPullRequest in cacheManager.GitHubPullRequests.Values)
            {
                Client = CreateGitHubClient(existingPullRequest.RepositoryName);
                GitHubPullRequest? pr = await GetPullRequestByPrNumberAsync(Client, existingPullRequest.RepositoryOwner, existingPullRequest.RepositoryName, existingPullRequest.Number);
                if (pr == null)
                {
                    continue;
                }

                cacheManager.GitHubPullRequests.AddOrUpdate(pr.Number, pr, (_, _) => pr);
            }
        }

        #endregion
    }
}
