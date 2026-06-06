using ChasmaWebApi.Core.Interfaces.Index;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Services.Index
{
    /// <summary>
    /// Service responsible for indexing local git repositories on the machine running this application and caching their information for use by other services. This service searches the logical drives on the machine for git repositories, validates them, and adds them to the cache if they are not already present. The cache is used to avoid expensive file system operations in the future and to provide quick access to repository information for other services.
    /// </summary>
    public class RepositoryIndexService : IRepositoryIndexService
    {
        /// <summary>
        /// Gets the logger instance for this service.
        /// </summary>
        private readonly ILogger<RepositoryIndexService> Logger;

        /// <summary>
        /// Provides access to the cache management functionality used by this service.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// The lock object used for concurrency.
        /// </summary>
        private readonly object lockObject = new();

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryIndexService"/> class.
        /// </summary>
        /// <param name="logger">The logger for this service.</param>
        /// <param name="cacheManager">The internal API cache manager.</param>
        public RepositoryIndexService(ILogger<RepositoryIndexService> logger, ICacheManager cacheManager)
        {
            Logger = logger;
            CacheManager = cacheManager;
        }

        #endregion

        // <inheritdoc/>
        public bool TryAddLocalGitRepositories(int userId, out List<LocalGitRepository> newRepositories)
        {
            newRepositories = new();
            List<Repository> validGitRepos = SearchLogicalDrivesForGitRepos();
            foreach (Repository repo in validGitRepos)
            {
                string workingDirectory = repo.Info.WorkingDirectory;
                LibGit2Sharp.Remote? remoteRepository = repo.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
                if (remoteRepository == null)
                {
                    Logger.LogWarning("Failed to find remote repository in {repoPath}, so it will not be added to cache.", workingDirectory);
                    continue;
                }

                string? repositoryName = GetRepositoryName(workingDirectory);
                if (string.IsNullOrWhiteSpace(repositoryName))
                {
                    Logger.LogWarning("Could get repository name for the file directory {path} so it will be ignored.", workingDirectory);
                    continue;
                }

                string repoCacheKey = Guid.NewGuid().ToString();
                lock (lockObject)
                {
                    if (CacheManager.WorkingDirectories.Values.Contains(workingDirectory))
                    {
                        Logger.LogWarning("Working directory {directory} already exists in cache, so it will be ignored.", workingDirectory);
                        continue;
                    }
                }

                string pushUrl = remoteRepository.PushUrl;
                if (string.IsNullOrEmpty(pushUrl))
                {
                    Logger.LogWarning("Failed to find push url for {repoName}, so it will be ignored.", repositoryName);
                    continue;
                }

                string repositoryOwner = GetRepositoryOwner(pushUrl);
                if (string.IsNullOrEmpty(repositoryOwner))
                {
                    Logger.LogWarning("Failed to find repository owner for {repoName}, so it will be ignored.", repositoryName);
                    continue;
                }

                RemoteHostPlatform platform = RemoteHelper.GetRemoteHostPlatform(pushUrl);
                LocalGitRepository localRepo = new()
                {
                    Id = repoCacheKey,
                    UserId = userId,
                    Name = repositoryName,
                    Owner = repositoryOwner,
                    Url = pushUrl,
                    HostPlatform = platform,
                };

                // Multiple instances of the same repository are allowed, but they must have different identifiers and working directories.
                if (CacheManager.Repositories.Values.Any(i => IsDuplicateRepository(localRepo, i, workingDirectory)))
                {
                    Logger.LogWarning("Repository {repoName} already exists in cache, so it will be ignored.", localRepo.GetDisplayName());
                    continue;
                }

                newRepositories.Add(localRepo);
                CacheManager.WorkingDirectories.TryAdd(localRepo.Id, workingDirectory);
            }

            foreach (LocalGitRepository repository in newRepositories)
            {
                if (CacheManager.Repositories.TryAdd(repository.Id, repository))
                {
                    Logger.LogInformation("Added repository {repoName} to cache for user {userId}.", repository.GetDisplayName(), userId);
                }
                else
                {
                    Logger.LogWarning("Cannot add duplicate repository with name {name} and id {id}", repository.GetDisplayName(), repository.Id);
                }
            }

            newRepositories = newRepositories.OrderBy(i => i.GetDisplayName()).ToList();
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
                    Logger.LogError(errorMessage);
                    return false;
                }

                repoName = repository.GetDisplayName();
                if (!CacheManager.WorkingDirectories.TryRemove(repositoryId, out string workingDirectory))
                {
                    errorMessage = $"Failed to find working directory for repository {repoName} in cache.";
                    Logger.LogError(errorMessage);
                    return false;
                }

                localGitRepositories = CacheManager.Repositories.Values
                    .Where(i => i.UserId == userId)
                    .OrderBy(i => i.GetDisplayName())
                    .ToList();
            }

            Logger.LogInformation("Successfully deleted repository {repoName} from cache.", repoName);
            return true;
        }

        // <inheritdoc/>
        public bool TryRemoveFile(RepositoryStatusElement selectedFile, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!CacheManager.WorkingDirectories.TryGetValue(selectedFile.RepositoryId, out string workingDirectory))
            {
                errorMessage = $"Failed to find working directory for repository with id {selectedFile.RepositoryId} in cache.";
                Logger.LogError("Failed to remove selected file and now sending error response because: {error}", errorMessage);
                return false;
            }

            try
            {
                using Repository repository = new(workingDirectory);
                if (selectedFile.IsStaged && !ShellUtility.TryExecuteShellCommand($"git restore --staged {selectedFile.FilePath}", workingDirectory, out errorMessage))
                {
                    errorMessage = $"Failed to unstage file: {errorMessage}";
                    Logger.LogError("Could not unstage changes when trying to delete file and now sending error response. Reason: {error}", errorMessage);
                    return false;
                }

                Commands.Remove(repository, selectedFile.FilePath);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while trying to delete the file {selectedFile.FilePath}: {e.Message}";
                Logger.LogError("An error occurred while trying to delete the file {filePath}: {error}. Sending error response.", selectedFile.FilePath, e);
                return false;
            }
        }

        // <inheritdoc/>
        public List<RepositoryAdditionResult> AddGitRepositories(IEnumerable<string> repoPaths, int userId, out List<NewRepository> newRepositories)
        {
            List<RepositoryAdditionResult> additionResults = [];
            newRepositories = [];
            foreach (string repoPath in repoPaths)
            {
                RepositoryAdditionResult additionResult = RegisterLocalRepository(repoPath, userId, out NewRepository addedRepo);
                additionResults.Add(additionResult);
                if (addedRepo != null)
                {
                    newRepositories.Add(addedRepo);
                }
            }

            return additionResults;
        }

        // <inheritdoc/>
        public List<RepositoryAdditionResult> CloneRepositories(IEnumerable<GitCloneBlueprint> blueprints, int userId, out List<NewRepository> newRepositories)
        {
            List<RepositoryAdditionResult> additionResults = [];
            newRepositories = [];
            ChasmaWebApiConfigurations apiConfiguration = ChasmaWebApiConfigurations.GetApiConfig();
            foreach (GitCloneBlueprint blueprint in blueprints)
            {
                try
                {
                    string sourceUrl = blueprint.SourceUrl;
                    if (sourceUrl.StartsWith("git@", StringComparison.OrdinalIgnoreCase) || sourceUrl.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase))
                    {
                        CloneRepositoryUsingSshProtocol(blueprint, userId, additionResults, newRepositories, apiConfiguration);
                    }
                    else if (sourceUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                    {
                        CloneRepositoryUsingHttpsProtocol(blueprint, userId, apiConfiguration, additionResults, newRepositories);
                    }
                    else
                    {
                        Logger.LogError("Encountered unexpected URL format {url} when trying to determine credentials for cloning. Will skip cloning attempt.", sourceUrl);
                        RepositoryAdditionResult additionResult = new()
                        {
                            IsSuccessful = false,
                            Reason = $"The provided repository URL {sourceUrl} is not in a recognized format for determining credentials. Supported formats are HTTPS and SSH URLs. Will have to manually clone from terminal.",
                            RepositoryName = Path.GetFileName(blueprint.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar)),
                        };
                        additionResults.Add(additionResult);
                    }
                }
                catch (Exception e)
                {
                    RepositoryAdditionResult additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = e.Message,
                        RepositoryName = !string.IsNullOrEmpty(blueprint.WorkingDirectory)
                            ? Path.GetFileName(blueprint.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar))
                            : "Unknown Repository",
                    };
                    additionResults.Add(additionResult);
                }
            }

            return additionResults;
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
                    Logger.LogWarning("The git path {path} could not be added due to an error, so it will be skipped: {error}. ", gitPath, e);
                }
            }

            return validatedRepositories;
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
        /// Gets the name of the repository directory from the specified repository path, excluding the ".git" suffix if
        /// present.
        /// </summary>
        /// <param name="repoPath">The full file system path to the repository directory.</param>
        /// <returns>The name of the repository directory with the ".git" suffix removed if present.</returns>
        private static string? GetRepositoryName(string repoPath) => new DirectoryInfo(repoPath).Name?.Replace(".git", "");

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
        /// Registers the addition of a new repository by validating the repository path, extracting necessary information, and adding it to the cache if valid.
        /// </summary>
        /// <param name="repoPath">The repository path.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="newRepo">The newly added repository to cache.</param>
        /// <returns>The repository addition result.</returns>
        private RepositoryAdditionResult RegisterLocalRepository(string repoPath, int userId, out NewRepository newRepo)
        {
            newRepo = null;
            RepositoryAdditionResult result = new();
            if (!Directory.Exists(repoPath))
            {
                Logger.LogError("Cannot add repository at path {path} because the path doesn't exist. Skipping addition or repo", repoPath);
                result.RepositoryName = Path.GetFileName(repoPath);
                result.IsSuccessful = false;
                result.Reason = $"The provided path {repoPath} does not exist and could not be created.";
                return result;
            }

            HashSet<string> workingDirectories;
            lock (lockObject)
            {
                workingDirectories = CacheManager.WorkingDirectories.Values.ToHashSet();
            }

            if (workingDirectories.Contains(repoPath))
            {
                Logger.LogError("Working directory {directory} already exists in cache, so it will be ignored when adding repository to the system.", repoPath);
                result.RepositoryName = Path.GetFileName(repoPath);
                result.IsSuccessful = false;
                result.Reason = $"The provided path {repoPath} already exists in the cache.";
                return result;
            }

            if (!Repository.IsValid(repoPath))
            {
                Logger.LogError("The provided path: {repoPath} is not a valid git repository when trying to add repository.", repoPath);
                result.RepositoryName = Path.GetFileName(repoPath);
                result.IsSuccessful = false;
                result.Reason = $"The provided path: {repoPath} is not a valid git repository.";
                return result;
            }

            string? repositoryName = GetRepositoryName(repoPath);
            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                Logger.LogError("Could get repository name for the file directory {path} so it cannot be added to cache when trying to add repository.", repoPath);
                result.RepositoryName = Path.GetFileName(repoPath);
                result.IsSuccessful = false;
                result.Reason = $"Could not get repository name for the file directory {repoPath}.";
                return result;
            }

            string pushUrl;
            try
            {
                using Repository repository = new(repoPath);
                LibGit2Sharp.Remote? remoteRepository = repository.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
                if (remoteRepository == null)
                {
                    Logger.LogError("Failed to find remote repository in {repoPath}, so it cannot be added to cache when trying to add repository.", repoPath);
                    result.RepositoryName = repositoryName;
                    result.IsSuccessful = false;
                    result.Reason = $"Failed to find remote repository in {repoPath}.";
                    return result;
                }

                pushUrl = remoteRepository.PushUrl;
                if (string.IsNullOrEmpty(pushUrl))
                {
                    Logger.LogError("Failed to find push url for {repoName}, so it cannot be added to cache when trying to add repository.", repositoryName);
                    result.RepositoryName = repositoryName;
                    result.IsSuccessful = false;
                    result.Reason = $"Failed to find push url for {repositoryName}.";
                    return result;
                }
            }
            catch (Exception e)
            {
                Logger.LogError("An error occurred while accessing the git repository at {repoPath}: {error}.", repoPath, e);
                result.RepositoryName = repositoryName;
                result.IsSuccessful = false;
                result.Reason = $"An error occurred while accessing the git repository at {repoPath}: {e.Message}";
                return result;
            }

            string? repositoryOwner = GetRepositoryOwner(pushUrl);
            if (string.IsNullOrEmpty(repositoryOwner))
            {
                Logger.LogError("Failed to find repository owner for {repoName}, so it cannot be added to cache when trying to add repository.", repositoryName);
                result.RepositoryName = repositoryName;
                result.IsSuccessful = false;
                result.Reason = $"Failed to find repository owner for {repositoryName}.";
                return result;
            }

            RemoteHostPlatform platform = RemoteHelper.GetRemoteHostPlatform(pushUrl);
            string repoCacheKey = Guid.NewGuid().ToString();
            LocalGitRepository localGitRepository = new()
            {
                Id = repoCacheKey,
                UserId = userId,
                Name = repositoryName,
                Owner = repositoryOwner,
                Url = pushUrl,
                HostPlatform = platform,
            };
            CacheManager.WorkingDirectories.TryAdd(localGitRepository.Id, repoPath);
            CacheManager.Repositories.TryAdd(localGitRepository.Id, localGitRepository);

            newRepo = new()
            {
                Repository = localGitRepository,
                WorkingDirectory = repoPath,
            };
            result.RepositoryName = repositoryName;
            result.IsSuccessful = true;
            return result;
        }

        /// <summary>
        /// Clones the specified repository using the SSH protocol.
        /// </summary>
        /// <param name="blueprint">The git clone blueprint.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="additionResults">The repository addition results.</param>
        /// <param name="newRepositories">The newly added git repositories.</param>
        /// <param name="apiConfigurations">The API configurations.</param>
        private void CloneRepositoryUsingSshProtocol(GitCloneBlueprint blueprint, int userId, ICollection<RepositoryAdditionResult> additionResults, ICollection<NewRepository> newRepositories, ChasmaWebApiConfigurations apiConfigurations)
        {
            string sourceUrl = blueprint.SourceUrl;
            RemoteHostPlatform remoteHostPlatform = RemoteHelper.GetRemoteHostPlatform(sourceUrl);
            string privateKeyPath;
            if (remoteHostPlatform == RemoteHostPlatform.GitHub)
            {
                privateKeyPath = apiConfigurations.GitHubSshKeyPrivateKeyPath;
            }
            else if (remoteHostPlatform == RemoteHostPlatform.GitLab)
            {
                privateKeyPath = apiConfigurations.GitLabSshKeyPrivateKeyPath;
            }
            else
            {
                privateKeyPath = string.Empty;
            }

            bool directoryCreated = false;
            string workingDirectory = blueprint.WorkingDirectory;
            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
                directoryCreated = true;
            }

            string recurseSubmodules = blueprint.RecurseSubmodules ? "--recurse-submodules" : string.Empty;
            string repoFolderName = Path.GetFileNameWithoutExtension(sourceUrl.TrimEnd('/'));
            string absoluteClonedRepositoryPath = Path.Combine(workingDirectory, repoFolderName);
            string cloneCommand = $"git -c core.sshCommand=\"ssh -i {privateKeyPath} -o IdentitiesOnly=yes\" clone {recurseSubmodules} {sourceUrl} \"{absoluteClonedRepositoryPath}\"";
            if (!ShellUtility.TryExecuteShellCommand(cloneCommand, workingDirectory, out string errorMessage))
            {
                RepositoryAdditionResult additionResult = new()
                {
                    IsSuccessful = false,
                    Reason = errorMessage,
                    RepositoryName = Path.GetFileName(absoluteClonedRepositoryPath.TrimEnd(Path.DirectorySeparatorChar)),
                };
                additionResults.Add(additionResult);
                if (directoryCreated)
                {
                    Directory.Delete(workingDirectory);
                }
            }
            else
            {
                RepositoryAdditionResult additionResult = RegisterLocalRepository(absoluteClonedRepositoryPath, userId, out NewRepository clonedRepo);
                additionResults.Add(additionResult);
                if (clonedRepo != null)
                {
                    newRepositories.Add(clonedRepo);
                }
            }
        }

        /// <summary>
        /// Clones the repository using the HTTPS protocol.
        /// </summary>
        /// <param name="blueprint">The git cloning repository template.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="apiConfiguration">The API configuration.</param>
        /// <param name="additionResults">The repository addition results.</param>
        /// <param name="newRepositories">The newly added git repositories.</param>
        private void CloneRepositoryUsingHttpsProtocol(GitCloneBlueprint blueprint, int userId, ChasmaWebApiConfigurations apiConfiguration, ICollection<RepositoryAdditionResult> additionResults, ICollection<NewRepository> newRepositories)
        {
            string remotePlatformUsername = null;
            string apiAccessToken = null;
            string workingDirectory = blueprint.WorkingDirectory;
            string sourceUrl = blueprint.SourceUrl;
            RemoteHostPlatform remoteHostPlatform = RemoteHelper.GetRemoteHostPlatform(sourceUrl);
            if (remoteHostPlatform == RemoteHostPlatform.GitHub)
            {
                remotePlatformUsername = apiConfiguration.GitHubUsername;
                apiAccessToken = apiConfiguration.GitHubApiToken;
            }
            else if (remoteHostPlatform == RemoteHostPlatform.GitLab)
            {
                remotePlatformUsername = apiConfiguration.GitLabUsername;
                apiAccessToken = apiConfiguration.GitLabApiToken;
            }
            
            try
            {
                CloneOptions cloneOptions = new()
                {
                    RecurseSubmodules = blueprint.RecurseSubmodules,
                    CredentialsProvider = (_url, _user, _cred) => GetUserPasswordCredentials(blueprint, apiConfiguration, apiAccessToken, remotePlatformUsername),
                };
                Repository.Clone(sourceUrl, workingDirectory, cloneOptions);
                RepositoryAdditionResult additionResult = RegisterLocalRepository(workingDirectory, userId, out NewRepository clonedRepo);
                additionResults.Add(additionResult);
                if (clonedRepo != null)
                {
                    newRepositories.Add(clonedRepo);
                }
            }
            catch (Exception e)
            {
                RepositoryAdditionResult additionResult = new()
                {
                    IsSuccessful = false,
                    Reason = $"An error occurred while trying to clone the repository: {e.Message}",
                    RepositoryName = Path.GetFileName(workingDirectory.TrimEnd(Path.DirectorySeparatorChar)),
                };
                additionResults.Add(additionResult);
            }
        }

        /// <summary>
        /// Gets the user-password credentials for HTTPS git cloning.
        /// </summary>
        /// <param name="blueprint">The git cloning blueprint details.</param>
        /// <param name="apiConfiguration">The API configuration.</param>
        /// <param name="apiAccessToken">The remote host platform API personal access token.</param>
        /// <param name="remotePlatformUsername">The remote host platform username (e.g., GitHub login username).</param>
        /// <returns>The user-password credentials.</returns>
        private static UsernamePasswordCredentials GetUserPasswordCredentials(GitCloneBlueprint blueprint, ChasmaWebApiConfigurations apiConfiguration, string apiAccessToken, string remotePlatformUsername)
        {
            // If we don't have an global API token, pass null to try an anonymous public clone.
            if (string.IsNullOrEmpty(apiAccessToken))
            {
                return null;
            }

            return new UsernamePasswordCredentials
            {
                // Fall back to standard "git" if username isn't saved. 
                // Most platforms accept any non-empty string when using access tokens.
                Username = !string.IsNullOrEmpty(remotePlatformUsername) ? remotePlatformUsername : "git",
                Password = apiAccessToken,
            };
        }

        #endregion
    }
}
