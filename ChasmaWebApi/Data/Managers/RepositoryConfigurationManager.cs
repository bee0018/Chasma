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
           
            string repositoryName = new DirectoryInfo(workingDirectory).Name?.Replace(".git", "");
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
                    // Allowed to have the same repos duplicated in cache, but it MUST be in different working directories.
                    continue;
                }
            }

            string pushUrl = remoteRepository.PushUrl;
            if (string.IsNullOrEmpty(pushUrl))
            {
                ClientLogger.LogWarning("Failed to find push url for {repoName}, so it will be ignored.", repositoryName);
                continue;
            }
            
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

            // Do not add duplicate repositories to cache.
            if (CacheManager.Repositories.Values.Any(i => RepositoriesMatch(localRepo, i))) 
            {
                ClientLogger.LogWarning("Repository {repoName} already exists in cache, so it will be ignored.", localRepo.Name);
                continue;
            }

            newRepositories.Add(localRepo);
            CacheManager.WorkingDirectories.TryAdd(localRepo.Id, workingDirectory);
        }

        foreach (LocalGitRepository repository in newRepositories)
        {
            CacheManager.Repositories.TryAdd(repository.Id, repository);
            ClientLogger.LogInformation("Added repository {repoName} to cache for user {userId}.", repository.Name, userId);
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
            if (!CacheManager.WorkingDirectories.TryRemove(repositoryId, out string _))
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

    /// <summary>
    /// Searches for git repositories on the logical drives on the machine running this application.
    /// </summary>
    /// <returns>The list of valid git repositories.</returns>
    private static List<Repository> SearchLogicalDrivesForGitRepos()
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
        
        return unvalidatedGitPaths
            .Where(Repository.IsValid)
            .Select(i => new Repository(i))
            .ToList();
    }

    /// <summary>
    /// Determines if two repositories match based on their properties.
    /// </summary>
    /// <param name="incomingRepo">The newly created repository.</param>
    /// <param name="existingRepo">The existing cached repository.</param>
    /// <returns>True if the repositories match; false otherwise.</returns>
    private static bool RepositoriesMatch(LocalGitRepository incomingRepo, LocalGitRepository existingRepo)
    {
        return incomingRepo.Name == existingRepo.Name &&
               incomingRepo.UserId == existingRepo.UserId &&
               incomingRepo.Owner == existingRepo.Owner &&
               incomingRepo.Url == existingRepo.Url;
    }
}