using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using LibGit2Sharp;
using Repository = LibGit2Sharp.Repository;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the manager for processing workflow run data.
/// </summary>
/// <param name="logger">The internal server logger.</param>
/// <param name="cacheManager">The internal API cache manager.</param>
public class RepositoryConfigurationManager(ILogger<RepositoryConfigurationManager> logger, ICacheManager cacheManager)
    : ClientManagerBase<RepositoryConfigurationManager>(logger, cacheManager), IRepositoryConfigurationManager
{
    /// <summary>
    /// The lock object used for concurrency.
    /// </summary>
    private readonly object lockObject = new();

    // <inheritdoc/>
    public bool TryAddLocalGitRepositories(int userId, out List<LocalGitRepository> newRepositories)
    {
        newRepositories = new();
        List<Repository> validGitRepos = SearchLogicalDrivesForGitRepos();
        foreach (Repository repo in validGitRepos)
        {
            string workingDirectory = repo.Info.WorkingDirectory;
            Remote? remoteRepository = repo.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
            if (remoteRepository == null)
            {
                ClientLogger.LogWarning("Failed to find remote repository in {repoPath}, so it will not be added to cache.", workingDirectory);
                continue;
            }

            string? repositoryName = GetRepositoryName(workingDirectory);
            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                ClientLogger.LogWarning("Could get repository name for the file directory {path} so it will be ignored.", workingDirectory);
                continue;
            }

            string repoCacheKey = Guid.NewGuid().ToString();
            lock (lockObject)
            {
                if (CacheManager.WorkingDirectories.Values.Contains(workingDirectory))
                {
                    ClientLogger.LogWarning("Working directory {directory} already exists in cache, so it will be ignored.", workingDirectory);
                    continue;
                }
            }

            string pushUrl = remoteRepository.PushUrl;
            if (string.IsNullOrEmpty(pushUrl))
            {
                ClientLogger.LogWarning("Failed to find push url for {repoName}, so it will be ignored.", repositoryName);
                continue;
            }

            string repositoryOwner = GetRepositoryOwner(pushUrl);
            if (string.IsNullOrEmpty(repositoryOwner))
            {
                ClientLogger.LogWarning("Failed to find repository owner for {repoName}, so it will be ignored.", repositoryName);
                continue;
            }

            LocalGitRepository localRepo = new LocalGitRepository
            {
                Id = repoCacheKey,
                UserId = userId,
                Name = repositoryName,
                Owner = repositoryOwner,
                Url = pushUrl,
            };

            // Multiple instances of the same repository are allowed, but they must have different identifiers and working directories.
            if (CacheManager.Repositories.Values.Any(i => IsDuplicateRepository(localRepo, i, workingDirectory)))
            {
                ClientLogger.LogWarning("Repository {repoName} already exists in cache, so it will be ignored.", localRepo.Name);
                continue;
            }

            newRepositories.Add(localRepo);
            CacheManager.WorkingDirectories.TryAdd(localRepo.Id, workingDirectory);
        }

        foreach (LocalGitRepository repository in newRepositories)
        {
            if (CacheManager.Repositories.TryAdd(repository.Id, repository))
            {
                ClientLogger.LogInformation("Added repository {repoName} to cache for user {userId}.", repository.Name, userId);
            }
            else
            {
                ClientLogger.LogWarning("Cannot add duplicate repository with name {name} and id {id}", repository.Name, repository.Id);
            }
        }

        newRepositories = newRepositories.OrderBy(i => i.Name).ToList();
        return newRepositories.Count > 0;
    }

    // <inheritdoc/>
    public bool TryDeleteRepository(string repositoryId, int userId, out List<LocalGitRepository> localGitRepositories, out string errorMessage)
    {
        localGitRepositories = new();
        errorMessage = string.Empty;
        string repoName;
        lock (lockObject)
        {
            if (!CacheManager.Repositories.TryRemove(repositoryId, out LocalGitRepository repository))
            {
                errorMessage = $"Failed to find repository with id {repositoryId} in cache.";
                ClientLogger.LogError(errorMessage);
                return false;
            }

            repoName = repository.Name;
            if (!CacheManager.WorkingDirectories.TryRemove(repositoryId, out string workingDirectory))
            {
                errorMessage = $"Failed to find working directory for repository {repoName} in cache.";
                ClientLogger.LogError(errorMessage);
                return false;
            }

            localGitRepositories = CacheManager.Repositories.Values
                .Where(i => i.UserId == userId)
                .OrderBy(i => i.Name)
                .ToList();
        }

        ClientLogger.LogInformation("Successfully deleted repository {repoName} from cache.", repoName);
        return true;
    }

    // <inheritdoc/>
    public bool TryDeleteBranch(string repositoryId, string branchName, out string errorMessage)
    {
        errorMessage = string.Empty;
        string workingDirectory = string.Empty;
        lock (lockObject)
        {
            if (!CacheManager.WorkingDirectories.TryGetValue(repositoryId, out workingDirectory))
            {
                errorMessage = $"No working directory could be found with repository identifier: {repositoryId}.";
                ClientLogger.LogError("Repository identifier {id} cannot be found and no working directory could be retrieved. Sending error response.", repositoryId);
                return false;
            }
        }

        using Repository repository = new(workingDirectory);
        Branch branchToDelete = repository.Branches.FirstOrDefault(i => i.FriendlyName == branchName);
        if (branchToDelete == null)
        {
            errorMessage = $"{branchName} does not exist in the repository.";
            ClientLogger.LogError("Repository identifier {id} cannot find branch with name {branchName}. Sending error response.", repositoryId, branchName);
            return false;
        }

        repository.Branches.Remove(branchToDelete);
        ClientLogger.LogInformation("Successfully deleted branch {branchName} from repository with id: {id}", branchName, repository);

        // Delete tracking of pull requests associated with this branch.
        List<int> pullRequestNumbersToDelete = CacheManager.GitHubPullRequests.Values
            .Where(i => i.BranchName == branchName)
            .Select(i => i.Number)
            .ToList();
        foreach (int prNumber in pullRequestNumbersToDelete)
        {
            CacheManager.GitHubPullRequests.TryRemove(prNumber, out _);
        }
        
        return true;
    }

    // <inheritdoc/>
    public bool TryAddGitRepository(string repoPath, int userId, out LocalGitRepository localGitRepository, out string errorMessage)
    {
        localGitRepository = null;
        errorMessage = string.Empty;
        if (!Directory.Exists(repoPath))
        {
            errorMessage = $"The provided path: {repoPath} does not exist.";
            ClientLogger.LogError(errorMessage);
            return false;
        }

        HashSet<string> workingDirectories;
        lock (lockObject)
        {
            workingDirectories = CacheManager.WorkingDirectories.Values.ToHashSet();
        }

        if (workingDirectories.Contains(repoPath))
        {
            errorMessage = $"The provided path {repoPath} already exists in the cache.";
            ClientLogger.LogWarning("Working directory {directory} already exists in cache, so it will be ignored.", repoPath);
            return false;
        }

        if (!Repository.IsValid(repoPath))
        {
            errorMessage = $"The provided path: {repoPath} is not a valid git repository.";
            ClientLogger.LogError(errorMessage);
            return false;
        }

        string? repositoryName = GetRepositoryName(repoPath);
        if (string.IsNullOrWhiteSpace(repositoryName))
        {
            errorMessage = $"Could not get repository name for the file directory {repoPath}.";
            ClientLogger.LogError("Could get repository name for the file directory {path} so it cannot be added to cache. Sending error response.", repoPath);
            return false;
        }

        string pushUrl;
        try
        {
            using Repository repository = new(repoPath);
            Remote? remoteRepository = repository.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
            if (remoteRepository == null)
            {
                errorMessage = $"Failed to find remote repository in {repoPath}.";
                ClientLogger.LogError("Failed to find remote repository in {repoPath}, so it cannot be added to cache.", repoPath);
                return false;
            }

            pushUrl = remoteRepository.PushUrl;
            if (string.IsNullOrEmpty(pushUrl))
            {
                errorMessage = $"Failed to find push url for {repositoryName}.";
                ClientLogger.LogError("Failed to find push url for {repoName}, so it cannot be added to cache.", repositoryName);
                return false;
            }
        }
        catch (Exception e)
        {
            errorMessage = $"An error occurred while accessing the git repository at {repoPath}: {e.Message}";
            ClientLogger.LogError("An error occurred while accessing the git repository at {repoPath}: {error}. Sending error response.", repoPath, e);
            return false;
        }

        string? repositoryOwner = GetRepositoryOwner(pushUrl);
        if (string.IsNullOrEmpty(repositoryOwner))
        {
            errorMessage = $"Failed to find repository owner for {repositoryName}.";
            ClientLogger.LogError("Failed to find repository owner for {repoName}, so it cannot be added to cache.", repositoryName);
            return false;
        }

        string repoCacheKey = Guid.NewGuid().ToString();
        localGitRepository = new()
        {
            Id = repoCacheKey,
            UserId = userId,
            Name = repositoryName,
            Owner = repositoryOwner,
            Url = pushUrl,
        };
        CacheManager.WorkingDirectories.TryAdd(localGitRepository.Id, repoPath);
        CacheManager.Repositories.TryAdd(localGitRepository.Id, localGitRepository);
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Searches for git repositories on the logical drives on the machine running this application.
    /// </summary>
    /// <returns>The list of valid git repositories.</returns>
    private List<Repository> SearchLogicalDrivesForGitRepos()
    {
        Stack<string> stack = new();
        List<string> roots = Directory.GetLogicalDrives().ToList();
        roots.ForEach(stack.Push);
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
                subDirectories.ForEach(stack.Push);
            }
            catch
            {
                // Ignore access errors
            }
        }

        List<Repository> validRepositories = GetValidatedRepositories(unvalidatedGitPaths);
        return validRepositories;
    }

    /// <summary>
    /// Determines if two repositories are duplicates of each other.
    /// </summary>
    /// <param name="incomingRepo">The newly created repository.</param>
    /// <param name="existingRepo">The existing cached repository.</param>
    /// <param name="incomingWorkingDirectory">The incoming working directory.</param>
    /// <returns>True if the repositories match; false otherwise.</returns>
    private bool IsDuplicateRepository(LocalGitRepository incomingRepo, LocalGitRepository existingRepo, string incomingWorkingDirectory)
    {
        if (incomingRepo.Id == existingRepo.Id)
        {
            return true;
        }

        string existingWorkingDirectory = CacheManager.WorkingDirectories[existingRepo.Id];
        if (incomingWorkingDirectory == existingWorkingDirectory)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the validated git repositories.
    /// </summary>
    /// <param name="unvalidatedGitPaths">The list of unvalidated repository paths.</param>
    /// <returns>The list of validated repositories.</returns>
    private List<Repository> GetValidatedRepositories(IEnumerable<string> unvalidatedGitPaths)
    {
        List<Repository> validatedRepositories = new();
        foreach (string gitPath in unvalidatedGitPaths)
        {
            try
            {
                if (!Repository.IsValid(gitPath))
                {
                    continue;
                }

                Repository repository = new(gitPath);
                validatedRepositories.Add(repository);
            }
            catch (Exception e)
            {
                ClientLogger.LogWarning("The git path {path} could not be added due to an error, so it will be skipped: {error}. ", gitPath, e);
            }
        }

        return validatedRepositories;
    }

    /// <summary>
    /// Gets the repository owner from the repository push URL.
    /// </summary>
    /// <param name="pushUrl">The push URL.</param>
    /// <returns>The repository owner.</returns>
    private static string GetRepositoryOwner(string pushUrl)
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

    /// <summary>
    /// Gets the name of the repository directory from the specified repository path, excluding the ".git" suffix if
    /// present.
    /// </summary>
    /// <param name="repoPath">The full file system path to the repository directory.</param>
    /// <returns>The name of the repository directory with the ".git" suffix removed if present.</returns>
    private static string? GetRepositoryName(string repoPath) => new DirectoryInfo(repoPath).Name?.Replace(".git", "");

    #endregion
}