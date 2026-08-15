using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Simulation;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Util;
using ChasmaWebApi.Util.Extensions;
using LibGit2Sharp;
using Octokit;
using System.Diagnostics;
using Branch = LibGit2Sharp.Branch;
using Commit = LibGit2Sharp.Commit;
using Repository = LibGit2Sharp.Repository;
using Signature = LibGit2Sharp.Signature;

namespace ChasmaWebApi.Core.Services.Git
{
    /// <summary>
    /// Service class containing the implementation of the members on the Git repository service, which is responsible for handling Git repository-level operations such as fetching branches and commits from a repository.
    /// </summary>
    public class GitRepositoryService : IGitRepositoryService
    {
        /// <summary>
        /// The logger instance for this service, used for logging information and errors related to Git repository operations.
        /// </summary>
        private readonly ILogger<GitRepositoryService> Logger;

        /// <summary>
        /// The cache manager, which is responsible for managing cached data such as repository statuses and GitHub pull request information to optimize performance of Git operations.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// The Git stash service, which is responsible for handling Git stash operations such as creating and applying stashes when taking work context snapshots.
        /// </summary>
        private readonly IGitStashService GitStashService;

        /// <summary>
        /// The Git branch service, which is responsible for handling branching operations.
        /// </summary>
        private readonly IGitBranchService GitBranchService;

        /// <summary>
        /// The simulation service, which is responsible for simulating Git operations.
        /// </summary>
        private readonly ISimulationService SimulationService;

        /// <summary>
        /// The internal API encryption service.
        /// </summary>
        private readonly IEncryptionService EncryptionService;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepositoryService"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record diagnostic and operational messages for the service.</param>
        /// <param name="cacheManager">The cache manager used to store and retrieve cached data for repository operations.</param>
        /// <param name="stashService">The Git stash service used to perform stash-related operations.</param>
        /// <param name="branchService">The Git branch service used to perform branch related operations.</param>
        /// <param name="simulationService">The Git simulation operations service.</param>
        /// <param name="encryptionService">The internal API encryption service for managing credential data.</param>
        public GitRepositoryService(ILogger<GitRepositoryService> logger, ICacheManager cacheManager, IGitStashService stashService, IGitBranchService branchService, ISimulationService simulationService, IEncryptionService encryptionService)
        {
            Logger = logger;
            CacheManager = cacheManager;
            GitStashService = stashService;
            GitBranchService = branchService;
            SimulationService = simulationService;
            EncryptionService = encryptionService;
        }

        #endregion

        // <inheritdoc/>
        public RepositorySummary? GetRepositoryStatus(string repoKey, Repository? existingRepo = null)
        {
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                Logger.LogError("Invalid repository key {repoKey} provided to get repository status.", repoKey);
                return null;
            }

            if (!CacheManager.Repositories.TryGetValue(repoKey, out LocalGitRepository localGitRepository))
            {
                Logger.LogError("Could not find local git repository with {repoKey} provided to get repository status.", repoKey);
                return null;
            }

            Repository repo = existingRepo ?? new(workingDirectory);
            try
            {
                RepositoryStatus status = repo.RetrieveStatus();
                List<RepositoryStatusElement> statusElements = GetWorkingTreeFiles(status, repoKey);
                Logger.LogDebug("Retrieved repository status for {repoKey} with {count} changes.", repoKey, statusElements.Count);
                BranchMetrics branchMetrics = GetBranchDiversionCalculation(workingDirectory, repo.Head.FriendlyName, localGitRepository);
                string remoteUrl = string.Empty;
                string commitHash = GetCommitHash(repo.Head, Logger);
                List<RemotePullRequest> remotePullRequests = [];
                if (localGitRepository.HostPlatform != RemoteHostPlatform.Local)
                {
                    remoteUrl = GetRemoteUrl(repo.Head, repo.Network.Remotes, workingDirectory) ?? string.Empty;
                    LibGit2Sharp.Remote? remoteOriginBranch = repo.Network.Remotes.FirstOrDefault(remote => remote.Name == "origin");
                    if (remoteOriginBranch == null)
                    {
                        Logger.LogWarning("Failed to find remote orign branch in {repoPath}, remote pull requests cannot be tracked.", workingDirectory);
                        remotePullRequests = new();
                    }
                    else
                    {
                        remotePullRequests = GetRemotePullRequests(localGitRepository.HostPlatform, branchMetrics.BranchName, repoKey);
                    }
                }

                RepositorySummary repositorySummary = new()
                {
                    RepositoryId = repoKey,
                    StatusElements = statusElements,
                    CommitsAhead = branchMetrics.AheadCount,
                    CommitsBehind = branchMetrics.BehindCount,
                    BranchName = branchMetrics.BranchName,
                    RemoteUrl = remoteUrl,
                    CommitHash = commitHash,
                    PullRequests = remotePullRequests,
                    LastUpdated = branchMetrics.LastUpdated,
                    IsUnborn = repo.Info.IsHeadUnborn,
                };
                return repositorySummary;
            }
            finally
            {
                if (existingRepo == null)
                {
                    repo.Dispose();
                }
            }
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyBulkStagingAction(string repoId, IEnumerable<string> fileNames, bool stagingFile)
        {
            List<RepositoryStatusElement> statusElements = new();
            if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
            {
                string paths = string.Join(", ", fileNames);
                Logger.LogError("Invalid repository key {repoKey} provided to stage the files {paths}.", repoId, paths);
                return statusElements;
            }

            if (!CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository localGitRepository))
            {
                string paths = string.Join(", ", fileNames);
                Logger.LogError("Repository not found with key {repoKey} when attemtping to stage/unstage the files {paths}.", repoId, paths);
                return statusElements;
            }

            using Repository repo = new(workingDirectory);
            foreach (string fileName in fileNames)
            {
                string stagingAction;
                if (stagingFile)
                {
                    stagingAction = "Staged";
                    Commands.Stage(repo, fileName);
                }
                else
                {
                    stagingAction = "Unstaged";
                    Commands.Unstage(repo, fileName);
                }

                Logger.LogInformation("{action} file {file}", stagingAction, fileName);
            }

            repo.Index.Write();
            RepositorySummary summary = GetRepositoryStatus(repoId, repo);
            return summary?.StatusElements;
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool stagingFile, string username, string token)
        {
            List<RepositoryStatusElement> statusElements = new();
            if (!CacheManager.WorkingDirectories.TryGetValue(repoKey, out string workingDirectory))
            {
                Logger.LogError("Invalid repository key {repoKey} provided to stage the file {fileName}.", repoKey, fileName);
                return statusElements;
            }

            using Repository repo = new(workingDirectory);
            string stagingAction;
            if (stagingFile)
            {
                stagingAction = "Staged";
                Commands.Stage(repo, fileName);
            }
            else
            {
                stagingAction = "Unstaged";
                Commands.Unstage(repo, fileName);
            }

            Logger.LogInformation("{action} file {file}", stagingAction, fileName);
            repo.Index.Write();
            RepositorySummary summary = GetRepositoryStatus(repoKey, repo);
            return summary?.StatusElements;
        }

        // <inheritdoc />
        public bool TryCommitChanges(string filePath, string fullName, string email, string commitMessage, string repoId, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repo = new(filePath);
                RepositoryStatus status = repo.RetrieveStatus();
                List<RepositoryStatusElement> workingTreeFiles = GetWorkingTreeFiles(status, repoId);
                if (!workingTreeFiles.Any(i => i.IsStaged))
                {
                    errorMessage = "No changes to commit.";
                    return false;
                }

                Signature author = new(fullName, email, DateTimeOffset.Now);
                repo.Commit(commitMessage, author, author);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Failed to commit changes for repository at {filePath}: {e.Message}";
                Logger.LogError(e, errorMessage);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryPushChanges(string filePath, LocalGitRepository repository, out string errorMessage)
        {
            errorMessage = string.Empty;
            using Repository repo = new(filePath);
            Branch branch = repo.Head;
            if (branch == null)
            {
                errorMessage = $"Failed to push changes. Could not get branch information for repository at {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            if (repo.Info.IsHeadDetached)
            {
                errorMessage = $"Failed to push changes. The HEAD is in a detached state for repository at {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            if (branch.TrackedBranch == null)
            {
                errorMessage = $"Failed to push changes. No upstream set for branch {branch.FriendlyName}.";
                Logger.LogError(errorMessage);
                return false;
            }

            try
            {
                PushOptions options = RemoteHelper.GetPushOptions(repository, EncryptionService);
                repo.Network.Push(branch, options);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to push changes automatically, trying manual push.");
                if (!ShellUtility.TryExecuteShellCommand("git push --no-verify", filePath, out string pushError))
                {
                    errorMessage = $"Failed to push changes to remote for branch {branch.FriendlyName}: {pushError}";
                    Logger.LogError(e, pushError);
                    return false;

                }

                return true;
            }
        }

        // <inheritdoc />
        public bool TryPullChanges(string workingDirectory, ApplicationUser user, LocalGitRepository localGitRepository, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repo = new(workingDirectory);
                Signature author = new(user.Name, user.Email, DateTimeOffset.Now);
                PullOptions options = RemoteHelper.GetPullOptions(localGitRepository, EncryptionService);
                Commands.Pull(repo, author, options);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogWarning("Failed to automatically pull changes. Attempting manual pull.");
                if (!ShellUtility.TryExecuteShellCommand("git pull", workingDirectory, out string pullError))
                {
                    errorMessage = $"Failed to pull changes from remote for repository at {workingDirectory}: {pullError}";
                    Logger.LogError(e, pullError);
                    return false;
                }

                return true;
            }
        }

        // <inheritdoc />
        public bool TryResetRepository(string workingDirectory, string revParseSpec, ResetMode resetMode, out string commitMessage, out string errorMessage)
        {
            errorMessage = string.Empty;
            commitMessage = string.Empty;
            try
            {
                string revision = !string.IsNullOrEmpty(revParseSpec) ? revParseSpec : "HEAD";
                using Repository repo = new(workingDirectory);
                repo.Reset(resetMode, revision);
                commitMessage = repo.Head.Tip.MessageShort;
                Logger.LogInformation("Successfully reset repository at {workingDirectory} to {revParseSpec} with reset mode {resetMode}.", workingDirectory, revParseSpec, resetMode);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Failed to reset repository to {revParseSpec} with reset mode {resetMode}. Check server logs for more information.";
                Logger.LogError(e, errorMessage);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryGetGitDiff(string workingDirectory, string filePath, bool isStaged, out string diffContent, out string errorMessage)
        {
            diffContent = string.Empty;
            if (!Directory.Exists(workingDirectory))
            {
                errorMessage = $"The working directory {workingDirectory} does not exist on filesystem. Cannot diff {filePath}.";
                Logger.LogError(errorMessage);
                return false;
            }

            using Repository repo = new(workingDirectory);
            RepositoryStatus updatedFilesInRepo = repo.RetrieveStatus();
            StatusEntry matchedFile = updatedFilesInRepo.FirstOrDefault(i => i.FilePath == filePath);
            if (matchedFile == null)
            {
                errorMessage = $"The file {filePath} does not exist in the changeset of this repository status";
                Logger.LogError("{error}. Sending error response.", errorMessage);
                return false;
            }

            string command = GetDiffCommand(isStaged, matchedFile.State, out bool isNewInWorkingDirectory);
            using Process process = ShellUtility.GetFileProcessingShell(command, filePath, workingDirectory);
            process.Start();
            diffContent = process.StandardOutput.ReadToEnd();
            errorMessage = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 && !isNewInWorkingDirectory)
            {
                /**
                 * git diff --no-index NUL return an error code of 1 so we added this check to make sure
                 * that we treat this as an error only when the file is not new in the working directory.
                 */
                errorMessage = $"Git diff command failed with exit code {process.ExitCode}. Error: {errorMessage}";
                Logger.LogError(errorMessage);
                return false;
            }

            return true;
        }

        // <inheritdoc />
        public bool TryGitRestore(RepositoryStatusElement selectedFile, out string errorMessage)
        {
            if (!CacheManager.WorkingDirectories.TryGetValue(selectedFile.RepositoryId, out string workingDirectory))
            {
                errorMessage = "Invalid repository key provided to restore the file.";
                Logger.LogError("Could not restore changes and now sending error response. Reason: {error}", errorMessage);
                return false;
            }

            if (selectedFile.IsStaged && !ShellUtility.TryExecuteShellCommand($"git restore --staged \"{selectedFile.FilePath}\"", workingDirectory, out errorMessage))
            {
                errorMessage = $"Failed to unstage file: {errorMessage}";
                Logger.LogError("Could not unstage changes when trying to restore file and now sending error response. Reason: {error}", errorMessage);
                return false;
            }

            return ShellUtility.TryExecuteShellCommand($"git restore \"{selectedFile.FilePath}\"", workingDirectory, out errorMessage);
        }

        // <inheritdoc />
        public List<RepositorySnapshotAdditionResult> AddWorkContextSnapshot(int userId, string snapshotDisplayName, IEnumerable<RepositorySnapshotBlueprint> blueprints, string? snapshotNote, out WorkContextSnapshot snapshot)
        {
            List<RepositorySnapshotAdditionResult> additionResults = [];
            List<RepsoitoryWorkContextSnapshotEntry> repoSnapshotEntries = [];
            foreach (RepositorySnapshotBlueprint blueprint in blueprints)
            {
                RepositorySnapshotAdditionResult additionResult;
                string errorMessage;
                string repoId = blueprint.RepositoryId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    errorMessage = $"Invalid repository key {repoId} provided to take snapshot.";
                    Logger.LogError("Failed to take repository snapshot due to: {reason}. Skipping snapshot creation.", errorMessage);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repoId,
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                if (!CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
                {
                    errorMessage = $"Failed to find repository in cache. Cannot save workspace context snapshot.";
                    Logger.LogError("Failed to take repository snapshot due to: {reason}. Skipping snapshot creation.", errorMessage);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repoId,
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                if (!CacheManager.Users.TryGetValue(userId, out ApplicationUser user))
                {
                    errorMessage = $"Current user does not exist, therefore cannot save workspace context snapshot..";
                    Logger.LogError("Failed to take repository snapshot due to: {reason}. Skipping snapshot creation.", errorMessage);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                string stashMessage = $"Snapshot stash {Guid.NewGuid()} for {snapshotDisplayName} at {DateTime.Now}";
                StashModifiers stashOption = StashModifiers.Default | StashModifiers.IncludeUntracked;
                if (!GitStashService.TryAddStash(workingDirectory, user, stashMessage, stashOption, out errorMessage))
                {
                    Logger.LogError("Failed to take repository snapshot due to: {reason}. Skipping snapshot creation.", errorMessage);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                try
                {
                    using Repository repo = new(workingDirectory);
                    int stashIndex = repo.Stashes.FindIndex(i => i.Message.Contains(stashMessage));
                    if (stashIndex != -1 && !GitStashService.TryApplyStash(workingDirectory, stashIndex, StashApplyModifiers.Default, out errorMessage))
                    {
                        errorMessage = $"Failed to reapply the stash for the repository {repository.GetDisplayName()} because: {errorMessage}";
                        Logger.LogError(errorMessage);
                        additionResult = new()
                        {
                            IsSuccessful = false,
                            Reason = errorMessage,
                            SnapshotName = snapshotDisplayName,
                            RepositoryName = repository.GetDisplayName(),
                        };
                        additionResults.Add(additionResult);
                        continue;
                    }

                    RepsoitoryWorkContextSnapshotEntry entry = new()
                    {
                        RepositoryId = repoId,
                        BranchName = repo.Head.FriendlyName,
                        CommitHash = repo.Head.Tip?.Sha ?? string.Empty,
                        CreatedAt = DateTimeOffset.Now.ToLocalTime().ToString("g"),
                        StashMessage = stashMessage,
                        IntentNote = blueprint.IntentNote,
                    };
                    repoSnapshotEntries.Add(entry);

                    additionResult = new()
                    {
                        IsSuccessful = true,
                        Reason = string.Empty,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                }
                catch (Exception e)
                {
                    errorMessage = $"Failed to create repository snapshot entry due to an exception: {e.Message}. Skipping snapshot creation.";
                    Logger.LogError(e, "Failed to take repository snapshot due to an exception. Skipping snapshot creation. Exception message: {message}", e.Message);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshotDisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                }
            }


            // After adding the workspace snapshot to the database, then the snapshot identifier will be set ensure synchronization between the database and cache.
            snapshot = new()
            {
                DisplayName = snapshotDisplayName,
                RepositorySnapshots = repoSnapshotEntries,
                SnapshotNote = snapshotNote,
                UserId = userId,
            };
            return additionResults;
        }

        // <inheritdoc />
        public List<RepositorySnapshotAdditionResult> LoadWorkspaceContextSnapshot(WorkContextSnapshot snapshot)
        {
            List<RepositorySnapshotAdditionResult> additionResults = [];
            foreach (RepsoitoryWorkContextSnapshotEntry repoEntry in snapshot.RepositorySnapshots)
            {
                RepositorySnapshotAdditionResult additionResult;
                string errorMessage;
                string repoId = repoEntry.RepositoryId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    errorMessage = $"Invalid repository key {repoId} provided to load snapshot.";
                    Logger.LogError("Failed to load repository snapshot due to: {reason}. Skipping snapshot loading: {name}.", errorMessage, snapshot.DisplayName);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshot.DisplayName,
                        RepositoryName = repoId,
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                if (!CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
                {
                    errorMessage = $"Could not find local git repository in the system with ID: {repoId}.";
                    Logger.LogError("Failed to load repository snapshot due to: {reason}. Skipping snapshot loading: {name}.", errorMessage, snapshot.DisplayName);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshot.DisplayName,
                        RepositoryName = repoId,
                    };
                    additionResults.Add(additionResult);
                    continue;
                }

                try
                {
                    using Repository repo = new(workingDirectory);
                    repo.Reset(ResetMode.Hard);
                    if (!GitBranchService.TryCheckoutBranch(workingDirectory, repoEntry.BranchName, BranchCheckoutMode.Default, null, out errorMessage))
                    {
                        errorMessage = $"Cannot load snapshot for this {repository.GetDisplayName()} because: {errorMessage}.";
                        Logger.LogError(errorMessage);
                        additionResult = new()
                        {
                            IsSuccessful = false,
                            Reason = errorMessage,
                            SnapshotName = snapshot.DisplayName,
                            RepositoryName = repository.GetDisplayName(),
                        };
                        additionResults.Add(additionResult);
                        continue;
                    }

                    Commit commit = repo.Commits.FirstOrDefault(i => i.Sha == repoEntry.CommitHash);
                    if (commit == null)
                    {
                        errorMessage = $"Failed to find the commit with hash {repoEntry.CommitHash} in the repository. Cannot fully load snapshot for {repository.GetDisplayName()}.";
                        Logger.LogError(errorMessage);
                        additionResult = new()
                        {
                            IsSuccessful = false,
                            Reason = errorMessage,
                            SnapshotName = snapshot.DisplayName,
                            RepositoryName = repository.GetDisplayName(),
                        };
                        additionResults.Add(additionResult);
                        continue;
                    }
                    
                    repo.Reset(ResetMode.Hard, commit);
                    int stashIndex = repo.Stashes.FindIndex(i => i.Message.Contains(repoEntry.StashMessage ?? string.Empty));
                    if (!GitStashService.TryApplyStash(workingDirectory, stashIndex, StashApplyModifiers.Default, out errorMessage))
                    {
                        errorMessage = $"Failed to apply the stash for the repository {repository.GetDisplayName()} because: {errorMessage}";
                        Logger.LogError(errorMessage);
                        additionResult = new()
                        {
                            IsSuccessful = false,
                            Reason = errorMessage,
                            SnapshotName = snapshot.DisplayName,
                            RepositoryName = repository.GetDisplayName(),
                        };
                        additionResults.Add(additionResult);
                        continue;
                    }

                    additionResult = new()
                    {
                        IsSuccessful = true,
                        Reason = string.Empty,
                        SnapshotName = snapshot.DisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                }
                catch (Exception e)
                {
                    errorMessage = $"Failed to load repository snapshot entry due to an exception: {e.Message}. Skipping snapshot creation for {snapshot.DisplayName}.";
                    Logger.LogError(e, "Failed to load repository snapshot due to an exception. Skipping snapshot creation for {name}. Exception message: {message}", snapshot.DisplayName, e.Message);
                    additionResult = new()
                    {
                        IsSuccessful = false,
                        Reason = errorMessage,
                        SnapshotName = snapshot.DisplayName,
                        RepositoryName = repository.GetDisplayName(),
                    };
                    additionResults.Add(additionResult);
                }
            }

            return additionResults;
        }

        // <inheritdoc />
        public RepositoryHealthScore GetHealthScore(string buildConclusion, RepositorySummary? repositoryMetrics, LocalGitRepository repository)
        {
            RepositoryHealthScore healthScore = new();
            if (repositoryMetrics  == null)
            {
                // If there are no repository metrics, we cannot calculate a health score, so we return 0.
                return healthScore;
            }

            SimulatedGitPullResult? pullSimulationResult = null;
            bool buildHasFailed = false;
            healthScore.Score = 100;
            if (IsFailedBuild(buildConclusion))
            {
                buildHasFailed = true;
                healthScore.Score -= 35;
            }

            int behindPenaltyWeight = 12;
            int aheadPenaltyWeight = 13;
            int dirtyFilesPenaltyWeight = 40;
            if (repositoryMetrics.CommitsBehind > 0)
            {
                PullSimulationEntry pullSimulationEntry = new()
                {
                    RepositoryId = repository.Id,
                    BranchToPull = repositoryMetrics.BranchName,
                };
                List<PullSimulationEntry> simulationEntries = [pullSimulationEntry];
                List<SimulatedGitPullResult> simulationResults = SimulationService.SimulateGitPull(simulationEntries);
                pullSimulationResult = simulationResults.FirstOrDefault();
                if (pullSimulationResult != null && !pullSimulationResult.IsSuccessful)
                {
                    behindPenaltyWeight = 40;
                    dirtyFilesPenaltyWeight = 12;
                    healthScore.Score -= behindPenaltyWeight;
                }
                else
                {
                    behindPenaltyWeight = 12;
                    dirtyFilesPenaltyWeight = 40;
                    int behindPenalty = repositoryMetrics.CommitsBehind * 2;
                    healthScore.Score -= Math.Min(behindPenalty, behindPenaltyWeight);
                }
            }

            int aheadPenalty = repositoryMetrics.CommitsAhead * 2;
            healthScore.Score -= Math.Min(aheadPenalty, aheadPenaltyWeight);

            int dirtyFilesPenalty = repositoryMetrics.StatusElements.Count * 2;
            healthScore.Score -= Math.Min(dirtyFilesPenalty, dirtyFilesPenaltyWeight);

            healthScore.Score = Math.Clamp(healthScore.Score, 0, 100);
            healthScore.ScoreCategory = GetHealthScoreTierName(healthScore.Score);
            healthScore.Description = GetHealthScoreDescription(healthScore.Score, buildHasFailed, repositoryMetrics, pullSimulationResult);
            return healthScore;
        }

        // <inheritdoc />
        public bool AreManifestFilesInChangeset(string workingDirectory)
        {
            try
            {
                using Repository repo = new(workingDirectory);
                Branch localBranch = repo.Head;
                if (localBranch?.Tip?.Tree == null)
                {
                    Logger.LogWarning("Cannot check for manifest files in changeset. Failed to get local branch tree information for repository at {workingDirectory}.", workingDirectory);
                    return false;
                }

                Branch remoteBranch = localBranch.TrackedBranch;
                if (remoteBranch?.Tip?.Tree == null)
                {
                    Logger.LogWarning("Cannot check for manifest files in changeset. Failed to get remote branch tree information for repository at {workingDirectory}.", workingDirectory);
                    return false;
                }

                Tree localTree = localBranch.Tip.Tree;
                Tree remoteTree = remoteBranch.Tip.Tree;
                TreeChanges changes = repo.Diff.Compare<TreeChanges>(remoteTree, localTree);
                HashSet<string> manifestFileExtensions = GetManifestFileExtensions();
                foreach (TreeEntryChanges? entry in changes)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    string fileExtension = Path.GetExtension(entry.Path);
                    if (manifestFileExtensions.Contains(fileExtension))
                    {
                        Logger.LogInformation("Found manifest file {file} in changeset for repository at {workingDirectory}.", entry.Path, workingDirectory);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to check for manifest files in changeset for repository at {workingDirectory}.", workingDirectory);
                return false;
            }
        }

        // <inheritdoc/>
        public bool TryInitializeRepository(InitializedRepositoryTemplate template, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repository = new(template.WorkingDirectory);
                if (!repository.Info.IsHeadUnborn)
                {
                    errorMessage = $"The repository is not in an unborn state and cannot be initialized.";
                    Logger.LogError("The repository at {workingDirectory} is not in an unborn state and cannot be initialized.", template.WorkingDirectory);
                    return false;
                }

                if (!ShellUtility.TryExecuteShellCommand($"git branch -m {template.HeadBranchName}", template.WorkingDirectory, out errorMessage))
                {
                    Logger.LogError("Failed to rename the default branch to {headBranchName} for repository at {workingDirectory} because: {error}.", template.HeadBranchName, template.WorkingDirectory, errorMessage);
                    return false;
                }

                RepositorySummary? summary = GetRepositoryStatus(template.Repository.Id);
                if (summary == null)
                {
                    errorMessage = "Failed to retrieve repository summary for the repository.";
                    Logger.LogError("Failed to retrieve repository summary for repository with id {repoId} when trying to initialize the repository.", template.Repository.Id);
                    return false;
                }

                if (summary.StatusElements.Count == 0)
                {
                    string repoName = template.Repository.GetDisplayName();
                    CreateIntialReadMeFile(template.WorkingDirectory, repoName);
                }

                Commands.Stage(repository, "*");
                Signature author = new(template.User.Name, template.User.Email, DateTimeOffset.Now);
                repository.Commit(template.CommitMessage, author, author);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "An error occurred while trying to initialize the repository. Review server logs for more information.";
                Logger.LogError("An error occurred while trying to initialize the repository at {workingDirectory}: {error}.", template.WorkingDirectory, e);
                return false;
            }
        }

        // <inheritdoc/>
        public LocalGitRepository ConnectRemoteRepository(InitializedRepositoryTemplate template, string headBranchName, string remoteUrl, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using Repository repo = new(template.WorkingDirectory);
                if (repo.Info.IsHeadUnborn && !TryInitializeRepository(template, out errorMessage))
                {
                    Logger.LogError("Failed to initialize the repository {repo} at {workingDirectory} when trying to connect to the remote repository because: {error}.", template.Repository.GetDisplayName(), template.WorkingDirectory, errorMessage);
                    return null;
                }

                if (!ShellUtility.TryExecuteShellCommand($"git branch -M {headBranchName}", template.WorkingDirectory, out errorMessage))
                {
                    errorMessage = "Failed to rename the default branch.";
                    Logger.LogError("Failed to rename the default branch to {headBranchName} for repository at {workingDirectory} because: {error}.", headBranchName, template.WorkingDirectory, errorMessage);
                    return null;
                }

                if (!ShellUtility.TryExecuteShellCommand($"git remote add origin {remoteUrl}", template.WorkingDirectory, out errorMessage))
                {
                    errorMessage = "Failed to add the remote repository.";
                    Logger.LogError("Failed to add the remote repository at {remoteUrl} for repository at {workingDirectory} because: {error}.", remoteUrl, template.WorkingDirectory, errorMessage);
                    return null;
                }

                if (!ShellUtility.TryExecuteShellCommand($"git push -u origin {headBranchName}", template.WorkingDirectory, out errorMessage))
                {
                    errorMessage = "Failed to push the initial commit to the remote repository.";
                    Logger.LogError("Failed to push the initial commit to the remote repository at {remoteUrl} for repository at {workingDirectory} because: {error}.", remoteUrl, template.WorkingDirectory, errorMessage);
                    return null;
                }

                Logger.LogInformation("Successfully connected to remote repository for repository {repo} with identifier {repoId}.", template.Repository.GetDisplayName(), template.Repository.Id);
                template.Repository.Url = remoteUrl;
                template.Repository.HostPlatform = RemoteHelper.GetRemoteHostPlatform(remoteUrl);
                template.Repository.Owner = RemoteHelper.GetRepositoryOwner(remoteUrl);
                return template.Repository;
            }
            catch (Exception e)
            {
                errorMessage = "An error occurred while trying to connect to the remote repository. Review server logs for more information.";
                Logger.LogError("An error occurred while trying to connect to the remote repository {repo} at {workingDirectory}: {error}.", template.Repository.GetDisplayName(), template.WorkingDirectory, e);
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// Determines if the file is staged based on its file status.
        /// </summary>
        /// <param name="fileStatus">The current file status.</param>
        /// <returns>True if the file is staged; false otherwise.</returns>
        private static bool IsFileStaged(FileStatus fileStatus, out bool hasUnstagedChanges)
        {
            hasUnstagedChanges = false;
            string[] statusStrings = fileStatus.ToString().Split(",");
            string[] trimmedStatusStrings = statusStrings.Select(i => i.Trim()).ToArray();
            FileStatus[] fileStatuses = trimmedStatusStrings.Select(Enum.Parse<FileStatus>).ToArray();
            bool isStaged = fileStatuses.Any(IsStateStaged);
            if (isStaged && fileStatuses.Any(state => state == FileStatus.ModifiedInWorkdir))
            {
                // The file is both staged and has unstaged changes.
                hasUnstagedChanges = true;
            }

            return isStaged;
        }

        /// <summary>
        /// Determines whether the file is staged in the repository.
        /// </summary>
        /// <param name="fileStatus">The current file status.</param>
        /// <returns>True if the file is in staged (in index); false otherwise.</returns>
        private static bool IsStateStaged(FileStatus fileStatus)
        {
            return fileStatus.HasFlag(FileStatus.NewInIndex) ||
                   fileStatus.HasFlag(FileStatus.ModifiedInIndex) ||
                   fileStatus.HasFlag(FileStatus.DeletedFromIndex) ||
                   fileStatus.HasFlag(FileStatus.RenamedInIndex) ||
                   fileStatus.HasFlag(FileStatus.TypeChangeInIndex);
        }

        /// <summary>
        /// Gets the remote URL for the specified repository.
        /// </summary>
        /// <param name="branch">The current checked out branch.</param>
        /// <param name="remoteBranches">The collection of remote branches.</param>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <returns>The remote URL of the repository.</returns>
        private string? GetRemoteUrl(Branch branch, RemoteCollection remoteBranches, string workingDirectory)
        {
            string remoteName = !string.IsNullOrEmpty(branch.RemoteName) ? branch.RemoteName : "origin";
            LibGit2Sharp.Remote? remote = remoteBranches[remoteName];
            if (remote == null)
            {
                Logger.LogWarning("Could not find remote {remoteName} for repository at {path}.", remoteName, workingDirectory);
                return null;
            }

            return remote.PushUrl ?? remote.Url;
        }

        /// <summary>
        /// Gets the commit hash for the specified branch.
        /// </summary>
        /// <param name="branch">The current branch.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The latest commit hash.</returns>
        private static string GetCommitHash(Branch branch, ILogger logger)
        {
            if (branch?.Tip == null)
            {
                logger.LogError("Cannot get commit hash. Failed to get branch information.");
                return string.Empty;
            }

            return GetCommitHash(branch.Tip, logger);
        }

        /// <summary>
        /// Gest the commit hash for the specified commit.
        /// </summary>
        /// <param name="commit">The commit.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The short commit hash.</returns>
        public static string GetCommitHash(Commit commit, ILogger logger)
        {
            if (commit == null)
            {
                logger.LogError("Cannot get commit hash. Commit information is null.");
                return string.Empty;
            }

            return commit.Sha.Length > 7 ? commit.Sha[..7] : commit.Sha;
        }

        /// <summary>
        /// Gets the diff command based on the file status and whether it is staged.
        /// </summary>
        /// <param name="isStaged">Flag indicating whether the file is staged.</param>
        /// <param name="fileStatus">The file status state.</param>
        /// <param name="isNewInWorkingDirectory">Flag indicating whether the file is new in the working directory.</param>
        /// <returns>The appropriate git diff command.</returns>
        private static string GetDiffCommand(bool isStaged, FileStatus fileStatus, out bool isNewInWorkingDirectory)
        {
            isNewInWorkingDirectory = false;
            if (fileStatus == FileStatus.NewInWorkdir)
            {
                isNewInWorkingDirectory = true;
                string emptyFilePlaceholderPath = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
                return $"git diff --no-index {emptyFilePlaceholderPath}";
            }
            else if (fileStatus == FileStatus.DeletedFromWorkdir || fileStatus == FileStatus.DeletedFromIndex)
            {
                return "git diff HEAD --";
            }
            else if (!isStaged)
            {
                return "git diff";
            }
            else
            {
                return "git diff --cached";
            }
        }

        /// <summary>
        /// Gets the remote pull request information for the specified branch.
        /// </summary>
        /// <param name="hostPlatform">The remote host platform.</param>
        /// <param name="branchName">The name of the branch that has a pull request created for it.</param>
        /// <param name="repoId">The repository identifier.</param>
        /// <returns>The list of pull request information for the specified branch.</returns>
        private List<RemotePullRequest> GetRemotePullRequests(RemoteHostPlatform hostPlatform, string branchName, string repoId)
        {
            if (hostPlatform == RemoteHostPlatform.GitHub)
            {
                return CacheManager.GitHubPullRequests.Values.Where(i => i.BranchName == branchName && i.RepositoryId == repoId).ToList();
            }

            if (hostPlatform == RemoteHostPlatform.GitLab)
            {
                return CacheManager.GitLabMergeRequests.Values.Where(i => i.BranchName == branchName && i.RepositoryId == repoId).ToList();
            }

            Logger.LogWarning("Could not get remote pull requests because the remote host platform {platform} is not supported.", hostPlatform);
            return new();
        }

        /// <summary>
        /// Determines if the build result is failed.
        /// </summary>
        /// <param name="buildConclusion">The workflow run build result.</param>
        /// <returns>True if the build has failed; false otherwise.</returns>
        private static bool IsFailedBuild(string buildConclusion)
        {
            if (string.IsNullOrEmpty(buildConclusion) || buildConclusion.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return buildConclusion.Equals("failure", StringComparison.OrdinalIgnoreCase) || buildConclusion.Equals("failed", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the health score tier name based on the score.
        /// </summary>
        /// <param name="score">The overall health score.</param>
        /// <returns>The health score tier name.</returns>
        private static string GetHealthScoreTierName(int score)
        {
            if (score >= 0 && score <= 59)
            {
                return "Severe";
            }

            if (score >= 60 && score <= 69)
            {
                return "Stalled";
            }

            if (score >= 70 && score <= 79)
            {
                return "Attention Needed";
            }

            if (score >= 80 && score <= 89)
            {
                return "Stable";
            }

            if (score >= 90 && score <= 99)
            {
                return "Great";
            }

            if (score == 100)
            {
                return "Excellent";
            }

            return "Unknown";
        }

        /// <summary>
        /// Generates the health score description based on the score, branch metrics, build result, and repository metrics.
        /// </summary>
        /// <param name="score">The overall repository score.</param>
        /// <param name="buildFailed">A value indicating whether the most recent build has failed.</param>
        /// <param name="repositoryMetrics">The repository metrics.</param>
        /// <param name="simulatedPullResult">The simulated git pull results.</param>
        /// <returns>The generated health score description.</returns>
        private static List<string> GetHealthScoreDescription(int score, bool buildFailed, RepositorySummary repositoryMetrics, SimulatedGitPullResult simulatedPullResult)
        {
            List<string> healthScoreDescription = [];
            if (score == 100)
            {
                healthScoreDescription.Add("Nothing to do");
                return healthScoreDescription;
            }

            if (buildFailed)
            {
                healthScoreDescription.Add("Most recent build failed. Manual review is needed.");
            }

            if (repositoryMetrics.CommitsBehind > 0 && repositoryMetrics.CommitsAhead > 0)
            {
                healthScoreDescription.Add("Diverged by being both ahead and behind.");
            }

            if (repositoryMetrics.CommitsBehind > 0)
            {
                if (simulatedPullResult != null && !simulatedPullResult.IsSuccessful)
                {
                    healthScoreDescription.Add("Need to pull in latest changes, but doing so would cause conflicts.");
                }
                else
                {
                    healthScoreDescription.Add("Need to pull in latest changes.");
                }
            }

            if (repositoryMetrics.CommitsAhead > 0)
            {
                healthScoreDescription.Add("There are commits ahead of the base and you need to your push changes to be fully up to date.");
            }

            if (repositoryMetrics.StatusElements.Count > 0)
            {
                healthScoreDescription.Add("There are uncommitted files in the working directory/index.");
            }

            return healthScoreDescription;
        }

        /// <summary>
        /// Gets the branch diversion calculation for the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The specified repository working directory.</param>
        /// <param name="branchName">The branch name.</param>
        /// <param name="repository">The local git repository.</param>
        /// <returns>The number of local branch name, commits ahead, behind, and last updated.</returns>
        private BranchMetrics GetBranchDiversionCalculation(string workingDirectory, string branchName, LocalGitRepository repository)
        {
            BranchMetrics branchMetrics = new();
            using Repository repo = new(workingDirectory);
            if (repo.Info.IsHeadUnborn)
            {
                branchMetrics.BranchName = $"Unborn HEAD: {repo.Head.FriendlyName}";
                branchMetrics.LastUpdated = string.Empty;
            }
            else
            {
                string lastHeadUpdateTimestamp = repo.Head.Commits
                .Max(i => i.Author.When)
                .ToLocalTime()
                .ToString("g");
                branchMetrics.LastUpdated = $"From HEAD: {lastHeadUpdateTimestamp}";
                branchMetrics.BranchName = (!repo.Branches.Any() ? "No HEAD branch has been set yet." : repo.Head?.FriendlyName) ?? string.Empty;
                Branch branch = repo.Branches.FirstOrDefault(i => i.FriendlyName == branchName);
                if (branch == null)
                {
                    Logger.LogError("Cannot get branch diversion calculation. Failed to get branch information for repository at {path}.", workingDirectory);
                    return branchMetrics;
                }

                if (repo.Info.IsHeadDetached)
                {
                    Logger.LogWarning("Cannot get detailed branch diversion calculation. The HEAD is in a detached state for repository at {path}.", workingDirectory);
                    string commitHash = GetCommitHash(branch, Logger);
                    branchMetrics.BranchName = $"(Detached HEAD at {commitHash})";
                    branchMetrics.LastUpdated = branch.Tip?.Committer.When.ToLocalTime().ToString("g") ?? "Unknown";
                    return branchMetrics;
                }

                string localBranchName = branch.FriendlyName;
                if (string.IsNullOrEmpty(localBranchName))
                {
                    Logger.LogError("Cannot get branch diversion calculation. No local branch found for repository at {path} with the branch name {branchName}.", workingDirectory, localBranchName);
                    return branchMetrics;
                }

                branchMetrics.BranchName = localBranchName;
                if (repository.HostPlatform == RemoteHostPlatform.Local)
                {
                    branchMetrics.LastUpdated = branch.Tip?.Committer.When.ToLocalTime().ToString("g") ?? "Unknown";
                }
                else
                {
                    if (branch.TrackedBranch == null)
                    {
                        Logger.LogWarning("Cannot get branch diversion calculation. Could not find the tracked branch for the local branch {branchName}.", localBranchName);
                        return branchMetrics;
                    }

                    string token = RemoteHelper.GetApiToken(repository.HostPlatform);
                    string decryptedToken = EncryptionService.DecryptString(token);
                    RemoteHelper.FetchLatestChanges(workingDirectory, branch, repository, Logger, decryptedToken);
                    string upstreamBranchName = branch.TrackedBranch.FriendlyName;
                    Branch localBranch = repo.Branches[localBranchName];
                    Branch upstreamBranch = repo.Branches[upstreamBranchName];
                    if (localBranch == null)
                    {
                        Logger.LogError("Cannot get detailed branch diversion calculation. No local branch with name {branchName} found.", localBranchName);
                        return branchMetrics;
                    }

                    if (upstreamBranch == null)
                    {
                        Logger.LogError("Cannot get detailed branch diversion calculation. No upstream branch with name {branchName} found.", upstreamBranchName);
                        return branchMetrics;
                    }

                    string lastUpdated = upstreamBranch.Tip.Committer.When.ToLocalTime().ToString("g");
                    HistoryDivergence divergence = repo.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, upstreamBranch.Tip);
                    branchMetrics = new()
                    {
                        BranchName = localBranchName,
                        AheadCount = divergence.AheadBy ?? 0,
                        BehindCount = divergence.BehindBy ?? 0,
                        LastUpdated = lastUpdated,
                    };
                }
            }

            return branchMetrics;
        }

        /// <summary>
        /// Gets the set of file extensions that are commonly used for manifest files.
        /// </summary>
        /// <returns>The set of manifest file extensions.</returns>
        private static HashSet<string> GetManifestFileExtensions()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".json",
                ".xml",
                ".yaml",
                ".yml",
                ".toml",
                ".ini",
                ".cfg",
                ".conf",
                ".properties",
                ".plist",
                ".manifest",
                ".appmanifest",
                ".webmanifest",
                ".mf",
                ".godot",
                ".uproject",
                ".unityproj",
                ".asmdef",
                ".csproj",
                ".vbproj",
                ".fsproj",
                ".sln",
                ".pubxml",
                ".nuspec",
                ".appxmanifest",
                ".podspec",
                ".pubspec",
                ".lock",
                ".gemspec",
                ".gemfile",
                ".gradle",
                ".pom",
                ".exs",
                ".nimble",
                ".opam",
                ".cabal",
                ".tf",
                ".tfvars",
                ".spec",
                ".ebextensions",
                ".project",
            };
        }

        /// <summary>
        /// Creates an initial README.md file in the specified working directory with a basic template if it doesn't already exist.
        /// </summary>
        /// <param name="workingDirectory">The working directory where the README.md file will be created.</param>
        /// <param name="repositoryName">The name of the repository.</param>
        private void CreateIntialReadMeFile(string workingDirectory, string repositoryName)
        {
            string readmeFilePath = Path.Combine(workingDirectory, "README.md");
            if (!File.Exists(readmeFilePath))
            {
                string readmeContent = $"""
                    
                    # {repositoryName}
                    
                    A short description of your project.

                    ## Getting Started
                    
                    1. Clone the repository.
                    2. Build and run.
                    
                    """;
                File.WriteAllText(readmeFilePath, readmeContent);
                Logger.LogInformation("Created initial README.md file for repository {repositoryName} at {readmeFilePath}.", repositoryName, readmeFilePath);
            }
            else
            {
                Logger.LogInformation("README.md file already exists for repository {repositoryName} at {readmeFilePath}. No action taken.", repositoryName, readmeFilePath);
            }
        }

        /// <summary>
        /// Gets the list of working tree files and their statuses for the specified repository status.
        /// </summary>
        /// <param name="status">The repository status.</param>
        /// <param name="repositoryId">The identifier of the repository.</param>
        /// <returns>A list of repository status elements.</returns>
        private static List<RepositoryStatusElement> GetWorkingTreeFiles(RepositoryStatus status, string repositoryId)
        {
            List<RepositoryStatusElement> statusElements = [];
            foreach (StatusEntry item in status)
            {
                FileStatus state = item.State;
                if (state == FileStatus.Ignored)
                {
                    // We only care about modified, deleted, and new files for now.
                    continue;
                }

                bool isStaged = IsFileStaged(state, out bool hasUnstagedChanges);
                RepositoryStatusElement statusElement = new()
                {
                    RepositoryId = repositoryId,
                    FilePath = item.FilePath,
                    State = item.State,
                    IsStaged = isStaged,
                };

                statusElements.Add(statusElement);
                if (hasUnstagedChanges)
                {
                    // Add another commitEntry for the unstaged changes.
                    RepositoryStatusElement unstagedElement = new()
                    {
                        RepositoryId = repositoryId,
                        FilePath = item.FilePath,
                        State = FileStatus.ModifiedInWorkdir,
                        IsStaged = false,
                    };

                    statusElements.Add(unstagedElement);
                }
            }

            return statusElements;
        }

        #endregion
    }
}
