using System.Collections.Concurrent;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using LibGit2Sharp;
using Octokit;
using Credentials = Octokit.Credentials;
using Repository = LibGit2Sharp.Repository;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the manager for processing workflow run data.
/// </summary>
/// <param name="logger">The internal server logger.</param>
public class WorkflowManager(ILogger<WorkflowManager> logger)
    : ClientManagerBase<WorkflowManager>(logger), IWorkFlowManager
{
    /// <summary>
    /// The lock object used for concurrency.
    /// </summary>
    private readonly object lockObject = new();
    
    // <inheritdoc/>
    public ConcurrentDictionary<string, LocalGitRepository> Repositories { get; } = new();
    
    // <inheritdoc/>
    public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
    {
        errorMessage = string.Empty;
        workflowRunResults = new();
        ProductHeaderValue productHeader = new ProductHeaderValue(repoName);
        Client = new(productHeader) { Credentials = new Credentials(token) };
        Task<WorkflowRunsResponse?> workflowRunsResponseTask = GetWorkFlowRuns(Client, repoOwner, repoName);
        WorkflowRunsResponse workFlowRunsResponse = workflowRunsResponseTask.Result;
        if (workFlowRunsResponse == null)
        {
            errorMessage = $"Failed to fetch workflow runs for {repoName}. Check server logs for more information.";
            return false;
        }
        
        List<WorkflowRun> runs = workFlowRunsResponse.WorkflowRuns.Take(buildCount).ToList();
        foreach (WorkflowRun run in runs)
        {
            WorkflowRunResult buildResult = new()
            {
                BranchName = run.HeadBranch,
                RunNumber = run.RunNumber,
                BuildTrigger = run.Event,
                CommitMessage = run.HeadCommit.Message,
                BuildStatus = run.Status.StringValue,
                BuildConclusion = run.Conclusion.HasValue ? run.Conclusion.Value.ToString() : "Unknown",
                CreatedDate = run.CreatedAt.ToString("g"),
                UpdatedDate = run.UpdatedAt.ToString("g"),
                WorkflowUrl = run.HtmlUrl,
                AuthorName = run.Actor.Login,
            };
            workflowRunResults.Add(buildResult);
        }
        
        ClientLogger.LogInformation("Retrieved {count} build runs from {repo}.", runs.Count, repoName);
        return true;
    }

    // <inheritdoc/>
    public List<string> FindLocalGitRepositories()
    {
        Stack<string> stack = new();
        List<string> roots = Directory.GetLogicalDrives().ToList();
        roots.ForEach(root => stack.Push(root));
        List<string> unvalidatedGitPaths = new();
        while (stack.Count > 0)
        {
            string path = stack.Pop();
            string gitPath = Path.Combine(path, ".git");
            try
            {
                if (File.Exists(gitPath) || Directory.Exists(gitPath))
                {
                    unvalidatedGitPaths.Add(path);
                    continue;
                }

                List<string> subDirectories = Directory.EnumerateDirectories(path).ToList();
                subDirectories.ForEach(sub => stack.Push(sub));
            }
            catch
            {
                // Ignore access errors
            }
        }
        
        List<Repository> validGitRepos = unvalidatedGitPaths.Where(Repository.IsValid).Select(i => new Repository(i)).ToList();
        foreach (Repository repo in validGitRepos)
        {
            string workingDirectory = repo.Info.WorkingDirectory;
            Remote? remoteRepository = repo.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
            if (remoteRepository == null)
            {
                ClientLogger.LogWarning("Failed to find remote repository in {repoPath}, so it will not be added to cache.", workingDirectory);
                continue;
            }
           
            string repositoryName = new DirectoryInfo(workingDirectory).Name?.Replace(".git", "");
            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                ClientLogger.LogWarning("Could get repository name for the file directory {path} so it will be ignored.", workingDirectory);
                continue;
            }

            LocalGitRepository existingRepository;
            lock (lockObject)
            {
                existingRepository = Repositories.Values.FirstOrDefault(i => i.RepositoryName == repositoryName);
            }
            
            if (existingRepository?.Repository.Info.WorkingDirectory == workingDirectory)
            {
                // Allowed to have the same repos duplicated in cache, but it MUST be in different working directories.
                continue;
            }

            string repoCacheKey = Guid.NewGuid().ToString();
            string repositoryOwner;
            string pushUrl = remoteRepository.PushUrl;
            if (string.IsNullOrEmpty(pushUrl))
            {
                ClientLogger.LogWarning("Failed to find push url for {repoName}, so it will be ignored.", repositoryName);
                continue;
            }
            
            if (pushUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                // HTTPS
                string[] httpParts = new Uri(pushUrl).AbsolutePath.Trim('/').Split('/');
                repositoryOwner = httpParts[0];
            }
            else
            {
                // SSH: git@github.com:OWNER/REPO.git
                string path = pushUrl.Split(':')[1];
                string[] sshParts = path.Split('/');
                repositoryOwner = sshParts[0];
            }
            
            if (string.IsNullOrEmpty(repositoryOwner))
            {
                ClientLogger.LogWarning("Failed to find repository owner for {repoName}, so it will be ignored.", repositoryName);
                continue;
            }
            
            LocalGitRepository localRepo = new LocalGitRepository
            {
                RepositoryName = repositoryName,
                RepositoryOwner = repositoryOwner,
                Repository = repo,
            };
            Repositories.TryAdd(repoCacheKey, localRepo);
        }
        
        ClientLogger.LogInformation("Found {totalCount} valid repo(s) on the filesystem.", Repositories.Count);
        return Repositories.Values.Select(i => i.RepositoryName).ToList();
    }

    /// <summary>
    /// Gets the workflow run for the specified repository.
    /// </summary>
    /// <param name="client">The GitHub API client.</param>
    /// <param name="repoOwner">The repository owner.</param>
    /// <param name="repoName">The repository name.</param>
    /// <returns>Task containing the workflow run response from the API client.</returns>
    private async Task<WorkflowRunsResponse?> GetWorkFlowRuns(GitHubClient client, string repoOwner, string repoName)
    {
        try
        {
            return await client.Actions.Workflows.Runs.List(repoOwner, repoName);
        }
        catch (Exception e)
        {
            ClientLogger.LogError("Error when trying to retrieve workflow runs for {repoName}: {error}", repoName, e);
            return null;
        }
    }
}