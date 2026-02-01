using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Octokit;
using YamlDotNet.Core.Tokens;

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
                LocalGitRepository repository = new LocalGitRepository()
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
                cacheManager.WorkingDirectories.TryAdd(workingDirectoryModel.RepositoryId, workingDirectoryModel.WorkingDirectory);
            }

            List<UserAccountModel> users = await applicationDbContext.UserAccounts.ToListAsync(cancellationToken);
            foreach (UserAccountModel user in users)
            {
                cacheManager.Users.TryAdd(user.Id, user);
            }

            logger.LogInformation("Finished updating the cache with the database data.");

            logger.LogInformation("Initializing the network cache with the Git client information.");
            _ = Task.Run(InitializeNetworkCacheAsync, cancellationToken);
        }

        /// <summary>
        /// Initializes the network cache with the Git client information.
        /// </summary>
        /// <returns>Task that initializes the cache with data from network clients.</returns>
        private async Task InitializeNetworkCacheAsync()
        {
            string apiToken = configurations.GitHubApiToken;
            if (string.IsNullOrEmpty(apiToken))
            {
                logger.LogWarning("GitHub API token is not provided. Skipping GitHub network cache initialization.");
                return;
            }

            try
            {
                foreach (LocalGitRepository repository in cacheManager.Repositories.Values)
                {
                    string repoName = repository.Name;
                    Client = new GitHubClient(new ProductHeaderValue(repoName))
                    {
                        Credentials = new Credentials(apiToken)
                    };

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
               
            }
            catch (Exception e)
            {
                logger.LogError("Error populating network cache: {error}", e);
            }
        }

        #region GitHub

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
                logger.LogError("Error when trying to get list of open pull request in {repoName}: {error}", repoName, e);
                return null;
            }
        }

        #endregion
    }
}
