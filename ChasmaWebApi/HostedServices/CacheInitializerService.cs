using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Util;
using Microsoft.EntityFrameworkCore;
using NGitLab;
using NGitLab.Models;
using Octokit;
using Project = NGitLab.Models.Project;

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
        private GitHubClient GitHubClient { get; set; }

        /// <summary>
        /// The GitLab API client.
        /// </summary>
        private GitLabClient GitLabClient { get; set; }

        /// <summary>
        /// The timer used to poll for GitHub pull request updates.
        /// </summary>
        private PeriodicTimer PullRequestPollTimer { get; set; }

        /// <summary>
        /// The timer used to poll for GitLab merge request updates.
        /// </summary>
        private PeriodicTimer MergeRequestPollTimer {  set; get; }

        // <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await InitializeInternalCacheAsync(cancellationToken);
        }

        // <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping the CacheInitializer hosted service and clearing cache.");
            cacheManager.WorkingDirectories.Clear();
            cacheManager.Repositories.Clear();
            cacheManager.Users.Clear();
            cacheManager.GitHubPullRequests.Clear();
            cacheManager.GitLabMergeRequests.Clear();
            PullRequestPollTimer?.Dispose();
            MergeRequestPollTimer?.Dispose();
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
                    HostPlatform = repoModel.HostPlatform,
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
            _ = Task.Run(async () =>
            {
                await InitializeNetworkCacheAsync(cancellationToken);
            }, cancellationToken);
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
            }
            else
            {
                await PopulateGitHubPullRequestCacheAsync(cancellationToken);
            }

            if (string.IsNullOrEmpty(configurations.GitLabApiToken))
            {
                logger.LogWarning("GitLab API token is not provided. Skipping GitHub network cache initialization.");
            }
            else
            {
                await GetGitLabMergeRequestsAsync(cancellationToken);
            }
        }

        #region GitHub

        /// <summary>
        /// Populates the GitHub Pull Request cache.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task of populating the GitHub pull request cache.</returns>
        private async Task PopulateGitHubPullRequestCacheAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Beginning Git network initialization using GitHub API.");
            try
            {
                // Start polling ONLY after GitHub pull requests are populated
                await FetchOpenGitHubPullRequestsAsync();
                StartPullRequestPolling(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError("Error populating GitHub network cache: {error}", e);
            }
        }

        /// <summary>
        /// Gets all the open pull requests via the GitHub API.
        /// </summary>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository owner.</param>
        /// <returns>Task containing the result of the API operation.</returns>
        private async Task<List<RemotePullRequest>?> GetGitHubPullRequestAsync(string owner, string repoName)
        {
            try
            {
                GitHubClient = RemoteHelper.GetGitHubClient(repoName, configurations.GitHubApiToken);
                IReadOnlyList<PullRequest> pullRequests = await GitHubClient.PullRequest.GetAllForRepository(owner, repoName);
                List<RemotePullRequest> gitHubPullRequests = [];
                foreach (PullRequest pullRequest in pullRequests)
                {
                    RemotePullRequest pr = new()
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
        /// <param name="owner">The repository owner.</param>
        /// <param name="name">The repoository name.</param>
        /// <param name="prNumber">The GitHub pull request number.</param>
        /// <returns>The internal GitHub pull request.</returns>
        private async Task<RemotePullRequest?> GetPullRequestByPrNumberAsync(string owner, string name, long prNumber)
        {
            try
            {
                GitHubClient = RemoteHelper.GetGitHubClient(name, configurations.GitHubApiToken);
                if (!int.TryParse(prNumber.ToString(), out int pullRequestNumber))
                {
                    logger.LogWarning("Cannot parse pull request number for {name}. Skipping.", name);
                    return null;
                }

                PullRequest? pullRequest = await GitHubClient.PullRequest.Get(owner, name, pullRequestNumber);
                if (pullRequest == null)
                {
                    return null;
                }

                RemotePullRequest pr = new()
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
                logger.LogWarning("Error when trying to get pull request #{mergeRequestId}: {error}", prNumber, e);
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
                        await RefreshPullRequestsAsync();
                        await FetchOpenGitHubPullRequestsAsync();
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
        /// </summary>
        private async Task RefreshPullRequestsAsync()
        {
            foreach (RemotePullRequest existingPullRequest in cacheManager.GitHubPullRequests.Values)
            {
                RemotePullRequest? pr = await GetPullRequestByPrNumberAsync(existingPullRequest.RepositoryOwner, existingPullRequest.RepositoryName, existingPullRequest.Number);
                if (pr == null)
                {
                    continue;
                }

                if (pr.Merged)
                {
                    cacheManager.GitHubPullRequests.TryRemove(pr.Number, out _);
                    logger.LogInformation("Stop tracking pull request {prId} because it has been merged.", pr.Number);
                    return;
                }

                StringEnum<ItemState> closedState = new(ItemState.Closed);
                if (pr.ActiveState == closedState.StringValue)
                {
                    cacheManager.GitHubPullRequests.TryRemove(pr.Number, out _);
                    logger.LogInformation("Stop tracking pull request {prId} because it has been closed.", pr.Number);
                    return;
                }

                cacheManager.GitHubPullRequests.AddOrUpdate(pr.Number, pr, (_, _) => pr);
            }
        }

        /// <summary>
        /// Gets the open pull requests for each of the GitHub repositories in cache.
        /// </summary>
        /// <returns>The task performing this task operation.</returns>
        private async Task FetchOpenGitHubPullRequestsAsync()
        {
            List<LocalGitRepository> gitHubRepositories = cacheManager.Repositories.Values
                    .Where(i => i.HostPlatform == RemoteHostPlatform.GitHub)
                    .ToList();
            foreach (LocalGitRepository repository in gitHubRepositories)
            {
                string repoName = repository.Name;
                List<RemotePullRequest>? pullRequests = await GetGitHubPullRequestAsync(repository.Owner, repository.Name);
                if (pullRequests == null)
                {
                    logger.LogWarning("No pull requests could be found for {name}.", repoName);
                    continue;
                }

                pullRequests.ForEach(pr =>
                {
                    cacheManager.GitHubPullRequests.TryAdd(pr.Number, pr);
                });
            }
        }

        #endregion

        #region GitLab

        /// <summary>
        /// Gets the merge request asynchronously via GitLab API.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task getting GitLab merge request data.</returns>
        private async Task GetGitLabMergeRequestsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Start polling ONLY after GitLab merge requests are populated
                await FetchOpenGitLabMergeRequestsAsync();
                StartMergeRequestPolling(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError("Error populating GitLab network cache: {error}", e);
            }
        }

        /// <summary>
        /// Gets all the open merge requests via the GitLab API.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <returns>Task containing the result of the API operation.</returns>
        private async Task<List<RemotePullRequest>?> GetGitLabMergeRequestAsync(LocalGitRepository repository)
        {
            try
            {
                GitLabClient = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                string owner = repository.Owner;
                string repoName = repository.Name;
                Project project = await GitLabClient.Projects.GetAsync($"{owner}/{repoName}");
                if (project == null)
                {
                    logger.LogError("Could not find project on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                IMergeRequestClient mergeRequestClient = GitLabClient.GetMergeRequest(project.Id);
                if (mergeRequestClient == null)
                {
                    logger.LogError("Could not find merge request client on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                List<MergeRequest> mergeRequests = mergeRequestClient.AllInState(MergeRequestState.opened).ToList();
                List<RemotePullRequest> gitLabMergeRequests = [];
                foreach (MergeRequest mergeRequest in mergeRequests)
                {
                    RemotePullRequest mr = new()
                    {
                        Number = mergeRequest.Iid,
                        RepositoryName = repoName,
                        RepositoryOwner = owner,
                        BranchName = mergeRequest.SourceBranch,
                        ActiveState = mergeRequest.State,
                        MergeableState = mergeRequest.MergeStatus,
                        CreatedAt = mergeRequest.CreatedAt.ToLocalTime().ToString("g"),
                        MergedAt = mergeRequest.MergedAt.HasValue ? mergeRequest.MergedAt.Value.ToLocalTime().ToString("g") : null,
                        Merged = mergeRequest.MergedAt.HasValue,
                        HtmlUrl = mergeRequest.WebUrl
                    };
                    gitLabMergeRequests.Add(mr);
                }

                return gitLabMergeRequests;
            }
            catch (Exception e)
            {
                logger.LogWarning("Error when trying to get list of open merge requests in {repoName}: {error}", repository.Name, e);
                return null;
            }
        }

        /// <summary>
        /// Starts the periodic polling of GitLab pull requests.
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        private void StartMergeRequestPolling(CancellationToken cancellationToken)
        {
            int intervalSeconds = configurations.GitLabMergeRequestScanIntervalSeconds;
            MergeRequestPollTimer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
            _ = Task.Run(async () =>
            {
                while (await MergeRequestPollTimer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        await RefreshMergeRequestsAsync(cancellationToken);
                        await FetchOpenGitLabMergeRequestsAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error while polling GitLab merge requests: {error}", ex);
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Refreshes the GitLab merge request cache.
        /// <param name="cancellationToken">The cancellation token.</param>
        /// </summary>
        private async Task RefreshMergeRequestsAsync(CancellationToken cancellationToken)
        {
            foreach (RemotePullRequest existingPullRequest in cacheManager.GitLabMergeRequests.Values)
            {
                string owner = existingPullRequest.RepositoryOwner;
                string repoName = existingPullRequest.RepositoryName;
                long iid = existingPullRequest.Number;
                RemotePullRequest? mr = await GetMergeRequestByIidNumberAsync(owner, repoName, iid, cancellationToken);
                if (mr == null)
                {
                    continue;
                }

                if (mr.Merged)
                {
                    cacheManager.GitLabMergeRequests.TryRemove(mr.Number, out _);
                    logger.LogInformation("Stop tracking merge request {mrId} because it has been merged.", mr.Number);
                    return;
                }

                if (mr.ActiveState == "closed")
                {
                    cacheManager.GitLabMergeRequests.TryRemove(mr.Number, out _);
                    logger.LogInformation("Stop tracking merge request {mrId} because it has been closed.", mr.Number);
                    return;
                }

                cacheManager.GitLabMergeRequests.AddOrUpdate(mr.Number, mr, (_, _) => mr);
            }
        }

        /// <summary>
        /// Gets the merge request by its internal identification via the GitLab API.
        /// </summary>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repoository name.</param>
        /// <param name="mergeRequestId">The GitLab merge request number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The internal remote pull request.</returns>
        private async Task<RemotePullRequest?> GetMergeRequestByIidNumberAsync(string owner, string repoName, long mergeRequestId, CancellationToken cancellationToken)
        {
            try
            {
                GitLabClient = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                Project project = await GitLabClient.Projects.GetAsync($"{owner}/{repoName}", cancellationToken: cancellationToken);
                if (project == null)
                {
                    logger.LogError("Could not find project on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                IMergeRequestClient mergeRequestClient = GitLabClient.GetMergeRequest(project.Id);
                if (mergeRequestClient == null)
                {
                    logger.LogError("Could not find merge request client on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                SingleMergeRequestQuery query = new();
                MergeRequest? mergeRequest = await mergeRequestClient.GetByIidAsync(mergeRequestId, query, cancellationToken);
                if (mergeRequest == null)
                {
                    logger.LogError("Could not find merge request with internal identifier: {id}", mergeRequestId);
                    return null;
                }

                RemotePullRequest mr = new()
                {
                    Number = mergeRequest.Iid,
                    RepositoryName = repoName,
                    RepositoryOwner = owner,
                    BranchName = mergeRequest.SourceBranch,
                    ActiveState = mergeRequest.State,
                    MergeableState = mergeRequest.MergeStatus,
                    CreatedAt = mergeRequest.CreatedAt.ToLocalTime().ToString("g"),
                    MergedAt = mergeRequest.MergedAt.HasValue ? mergeRequest.MergedAt.Value.ToLocalTime().ToString("g") : null,
                    Merged = mergeRequest.MergedAt.HasValue,
                    HtmlUrl = mergeRequest.WebUrl
                };
                return mr;
            }
            catch (Exception e)
            {
                logger.LogWarning("Error when trying to get pull request #{mergeRequestId}: {error}", mergeRequestId, e);
                return null;
            }
        }

        /// <summary>
        /// Fetches the open GitLab merge requests for each of the GitLab repositories.
        /// </summary>
        /// <returns>Task return the fetch operation task.</returns>
        private async Task FetchOpenGitLabMergeRequestsAsync()
        {
            List<LocalGitRepository> gitLabRepositories = cacheManager.Repositories.Values
                    .Where(i => i.HostPlatform == RemoteHostPlatform.GitLab)
                    .ToList();
            foreach (LocalGitRepository repository in gitLabRepositories)
            {
                List<RemotePullRequest>? mergeRequests = await GetGitLabMergeRequestAsync(repository);
                if (mergeRequests == null)
                {
                    logger.LogWarning("No merge requests could be found for {name}.", repository.Name);
                    continue;
                }

                mergeRequests.ForEach(mr =>
                {
                    cacheManager.GitLabMergeRequests.TryAdd(mr.Number, mr);
                });
            }
        }

        #endregion
    }
}
