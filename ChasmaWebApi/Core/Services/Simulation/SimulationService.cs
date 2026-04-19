using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Simulation;
using ChasmaWebApi.Core.Services.Git;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Services.Simulation
{
    /// <summary>
    /// Class responsible simulating git operations.
    /// </summary>
    public class SimulationService : ISimulationService
    {
        /// <summary>
        /// The logger instance for this service, used for logging information and errors related to Git repository operations.
        /// </summary>
        private readonly ILogger<SimulationService> Logger;

        /// <summary>
        /// The cache manager, which is responsible for managing cached data such as repository statuses and GitHub pull request information to optimize performance of Git operations.
        /// </summary>
        private readonly ICacheManager CacheManager;

        /// <summary>
        /// The Git branch service, which is responsible for handling branch related operations.
        /// </summary>
        private readonly IGitBranchService GitBranchService;

        /// <summary>
        /// The Git stash service, which is responsible for handling Git stash operations.
        /// </summary>
        private readonly IGitStashService GitStashService;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationService"/> class.
        /// </summary>
        /// <param name="logger">The logging instance.</param>
        /// <param name="cacheManager">The internal cache manager.</param>
        /// <param name="gitBranchService">The branch service.</param>
        public SimulationService(ILogger<SimulationService> logger, ICacheManager cacheManager, IGitBranchService gitBranchService, IGitStashService gitStashService)
        {
            Logger = logger;
            CacheManager = cacheManager;
            GitBranchService = gitBranchService;
            GitStashService = gitStashService;
        }

        #endregion

        // <inheritdoc />
        public List<SimulatedGitPullResult> SimulateGitPull(IEnumerable<PullSimulationEntry> entries)
        {
            Branch tempBranch = null;
            List<SimulatedGitPullResult> dryRunResults = new();
            foreach (PullSimulationEntry entry in entries)
            {
                string repoId = entry.RepositoryId;
                SimulatedGitPullResult simulatedPullResult = new();
                simulatedPullResult.RepositoryName = CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository) ? repository.Name : repoId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    Logger.LogError("Invalid repository key {repoKey} provided to simulate git pull.", repoId);
                    DryRunHelper.FailSimulationResult(simulatedPullResult, $"Invalid repository key {repoId}. Add or ignore repository.");
                    dryRunResults.Add(simulatedPullResult);
                    continue;
                }

                string branchToPull = entry.BranchToPull;
                using Repository repo = new(workingDirectory);
                Branch localBranch = string.IsNullOrEmpty(branchToPull) ? repo.Head : repo.Branches[branchToPull];
                if (localBranch == null)
                {
                    Logger.LogError("Failed to simulate git pull for repository {repoId}. Could not get branch information for repository at {path}.", repoId, workingDirectory);
                    DryRunHelper.FailSimulationResult(simulatedPullResult, "Failed to get branch information. There is not a HEAD configured for this repository.");
                    dryRunResults.Add(simulatedPullResult);
                    continue;
                }

                simulatedPullResult.BranchName = localBranch.FriendlyName;
                if (repo.Info.IsHeadDetached)
                {
                    Logger.LogWarning("Failed to simulate git pull for repository {repoId}. The HEAD is in a detached state for repository at {path}.", repoId, workingDirectory);
                    DryRunHelper.FailSimulationResult(simulatedPullResult, "The HEAD is in a detached state (pointed at a specific commit). You can pull if you explicitly specify the branch.");
                    dryRunResults.Add(simulatedPullResult);
                    continue;
                }

                string friendlyBranchName = localBranch.FriendlyName;
                if (localBranch.TrackedBranch == null)
                {
                    Logger.LogWarning("Failed to simulate git pull for repository {repoId}. No upstream set for branch {branchName}.", repoId, friendlyBranchName);
                    DryRunHelper.FailSimulationResult(simulatedPullResult, $"No upstream set for branch {friendlyBranchName}. Set the upstream branch first or you can pull if you explicitly specify the branch.");
                    dryRunResults.Add(simulatedPullResult);
                    continue;
                }

                try
                {
                    CommitFilter commitFilter = new()
                    {
                        IncludeReachableFrom = localBranch.TrackedBranch,
                        ExcludeReachableFrom = localBranch
                    };
                    List<Commit> commitsToPull = repo.Commits.QueryBy(commitFilter).ToList();
                    List<CommitEntry> commitEntries = new();
                    foreach (Commit commit in commitsToPull)
                    {
                        string commitHash = GitRepositoryService.GetCommitHash(commit, Logger);
                        CommitEntry commitEntry = new()
                        {
                            CommitHash = commitHash,
                            Message = commit.MessageShort,
                        };
                        commitEntries.Add(commitEntry);
                    }

                    simulatedPullResult.CommitsToPull = commitEntries;
                    string tempBranchName = $"temp-dry-run-{Guid.NewGuid():N}";
                    tempBranch = repo.CreateBranch(tempBranchName, localBranch.Tip);
                    if (!GitBranchService.TryCheckoutBranch(workingDirectory, tempBranchName, out string checkoutError))
                    {
                        Logger.LogError("Failed to checkout temporary branch {tempBranch} for repository {repoId} during git pull simulation. Error: {error}", tempBranchName, repoId, checkoutError);
                        DryRunHelper.FailSimulationResult(simulatedPullResult, $"Failed to checkout temporary branch for simulating pull: {checkoutError}");
                        dryRunResults.Add(simulatedPullResult);
                        repo.Branches.Remove(tempBranch);
                        continue;
                    }

                    MergeOptions mergeOptions = new()
                    {
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                        FailOnConflict = true
                    };
                    Signature tempAuthor = new("DryRun", "dryrun@example.com", DateTimeOffset.Now);
                    MergeResult mergeResult = repo.Merge(localBranch.TrackedBranch.Tip, tempAuthor, mergeOptions);
                    if (mergeResult.Status == MergeStatus.Conflicts)
                    {
                        DryRunHelper.FailSimulationResult(simulatedPullResult, "Pull would fail due to merge conflicts. Ensure changes on your branch will not interfere with version on tracked branch.");
                    }
                    else
                    {
                        simulatedPullResult.IsSuccessful = true;
                    }

                    if (!GitBranchService.TryCheckoutBranch(workingDirectory, friendlyBranchName, out string error))
                    {
                        Logger.LogError("Failed to checkout back to original branch {branchName} for repository {repoId} after git pull simulation. Error: {error}", friendlyBranchName, repoId, error);
                        DryRunHelper.FailSimulationResult(simulatedPullResult, $"Failed to checkout back to original branch after simulating pull: {error}");
                        dryRunResults.Add(simulatedPullResult);
                        repo.Branches.Remove(tempBranch);
                        continue;
                    }

                    dryRunResults.Add(simulatedPullResult);
                    repo.Branches.Remove(tempBranch);
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to simulate git pull for repository {repoId}: {message}.", repoId, e);
                    DryRunHelper.FailSimulationResult(simulatedPullResult, "Pull would fail due to conflicts. Ensure local changes will merge into tracked branch successfully.");
                    dryRunResults.Add(simulatedPullResult);
                    if (!GitBranchService.TryCheckoutBranch(workingDirectory, friendlyBranchName, out string checkoutError))
                    {
                        Logger.LogError("Failed to revert back to {originalBranch} for repository {repoId} during git pull simulation. Error: {error}", friendlyBranchName, repoId, checkoutError);
                        DryRunHelper.FailSimulationResult(simulatedPullResult, $"Failed to revert to {friendlyBranchName} for simulating pull: {checkoutError}");
                        dryRunResults.Add(simulatedPullResult);
                        repo.Branches.Remove(tempBranch);
                    }
                }
            }

            return dryRunResults;
        }

        // <inheritdoc />
        public List<SimulatedAddBranchResult> SimulateAddBranch(IEnumerable<AddBranchSimulationEntry> entries)
        {
            List<SimulatedAddBranchResult> dryRunResults = new();
            foreach (AddBranchSimulationEntry entry in entries)
            {
                string repoId = entry.RepositoryId;
                SimulatedAddBranchResult simulatedAddBranchResult = new();
                simulatedAddBranchResult.RepositoryName = CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository) ? repository.Name : repoId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    Logger.LogError("Invalid repository key {repoKey} provided to simulate adding a branch.", repoId);
                    DryRunHelper.FailSimulationResult(simulatedAddBranchResult, $"Invalid repository key {repoId}. Add or ignore repository.");
                    dryRunResults.Add(simulatedAddBranchResult);
                    continue;
                }

                using Repository repo = new(workingDirectory);
                string newBranchName = entry.BranchToAdd;
                if (string.IsNullOrEmpty(newBranchName))
                {
                    Logger.LogError("Failed to simulate adding branch because the branch name was not populated.");
                    DryRunHelper.FailSimulationResult(simulatedAddBranchResult, "Branch name must be populated");
                    dryRunResults.Add(simulatedAddBranchResult);
                    continue;
                }

                simulatedAddBranchResult.BranchToAdd = newBranchName;
                Branch existingBranch = repo.Branches[newBranchName];
                if (existingBranch != null)
                {
                    Logger.LogWarning("Failed to simulate adding branch {branchName} for repository {repoId}. A branch with the same name already exists.", newBranchName, repoId);
                    DryRunHelper.FailSimulationResult(simulatedAddBranchResult, $"A branch named {newBranchName} already exists. Choose a different name for the new branch.");
                    dryRunResults.Add(simulatedAddBranchResult);
                    continue;
                }

                simulatedAddBranchResult.IsSuccessful = true;
                simulatedAddBranchResult.InfoMessage = $"A new branch named {newBranchName} can be created successfully.";
                dryRunResults.Add(simulatedAddBranchResult);
            }

            return dryRunResults;
        }

        // <inheritdoc />
        public List<SimulatedMergeResult> SimulateMergeBranch(IEnumerable<MergeSimulationEntry> entries)
        {
            List<SimulatedMergeResult> dryRunResults = new();
            foreach (MergeSimulationEntry entry in entries)
            {
                SimulatedMergeResult simulatedMergeResult = new();
                string repoId = entry.RepositoryId;
                string sourceBranchName = entry.SourceBranch;
                string destinationBranchName = entry.DestinationBranch;
                simulatedMergeResult.RepositoryName = CacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository) ? repository.Name : repoId;
                if (!CacheManager.WorkingDirectories.TryGetValue(repoId, out string workingDirectory))
                {
                    Logger.LogError("Invalid repository key {repoKey} provided to simulate merging {source} into {destination}.", repoId, sourceBranchName, destinationBranchName);
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Invalid repository key {repoId}. Add or ignore repository.");
                    dryRunResults.Add(simulatedMergeResult);
                    continue;
                }

                using Repository repo = new(workingDirectory);
                string currentBranch = repo.Head.FriendlyName;
                Branch sourceBranch = repo.Branches[$"origin/{sourceBranchName}"];
                if (sourceBranch == null)
                {
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Source branch {sourceBranchName} does not exist. Merge cannot be performed.");
                    dryRunResults.Add(simulatedMergeResult);
                    Logger.LogError("Source branch {sourceBranchName} does not exist. Sending error response.", sourceBranchName);
                    continue;
                }

                Commit originalCommit = repo.Head.Tip;
                Branch destinationBranch = repo.Branches[destinationBranchName];
                if (destinationBranch == null)
                {
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Destination branch {destinationBranchName} does not exist. Merge cannot be performed.");
                    dryRunResults.Add(simulatedMergeResult);
                    Logger.LogError("Destination branch {destinationBranchName} does not exist. Sending error response.", destinationBranchName);
                    continue;
                }

                if (!CacheManager.Users.TryGetValue(entry.UserId, out ApplicationUser user))
                {
                    // Creating a dummy user since the user associated with the merge simulation entry cannot be found.
                    user = new()
                    {
                        UserId = entry.UserId,
                        Email = "chasma.bot@test.com",
                        Name = "chasma-bot",
                        UserName = "chasma.bot"
                    };
                }

                string stashMessage = $"merge simulation stash for merging {sourceBranchName} into {destinationBranchName}";
                if (!GitStashService.TryAddStash(workingDirectory, user, stashMessage, StashModifiers.Default, out string stashError))
                {
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Failed to stash changes before simulating merge. Error: {stashError}");
                    dryRunResults.Add(simulatedMergeResult);
                    Logger.LogError("Failed to stash changes before simulating merge of {sourceBranchName} into {destinationBranchName} for repository at {repoPath}. Error: {error}. Sending error response.", sourceBranchName, destinationBranchName, workingDirectory, stashError);
                    repo.Reset(ResetMode.Hard, originalCommit);
                    continue;
                }

                if (!GitBranchService.TryCheckoutBranch(workingDirectory, destinationBranchName, out string checkoutError))
                {
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Failed to checkout destination branch {destinationBranchName} before simulating merge. Error: {checkoutError}");
                    dryRunResults.Add(simulatedMergeResult);
                    Logger.LogError("Failed to checkout destination branch {destinationBranchName} before simulating merge of {sourceBranchName} into {destinationBranchName} for repository at {repoPath}. Error: {error}.", destinationBranchName, sourceBranchName, destinationBranchName, workingDirectory, checkoutError);
                    continue;
                }

                Signature author = new(user.Name, user.Email, DateTimeOffset.Now);
                MergeOptions mergeOptions = new()
                {
                    FailOnConflict = true,
                    CommitOnSuccess = false,

                };
                MergeResult mergeResult = repo.Merge(sourceBranch, author, mergeOptions);
                string repoName = repository != null ? repository.Name : repoId;
                simulatedMergeResult.MergeStatus = GetMergeStatusMessage(mergeResult.Status, sourceBranchName, destinationBranchName, repoName);
                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    string conflictPhrase = repo.Index.Conflicts.Any()
                        ? $" due to conflicts in the following files: {string.Join(", ", repo.Index.Conflicts.Select(i => i.Ours.Path))}"
                        : string.Empty;
                    DryRunHelper.FailSimulationResult(simulatedMergeResult, $"Merge from {sourceBranchName} to {destinationBranchName} cannot be performed{conflictPhrase}. Resolve potential conflicts and try merging again.");
                    simulatedMergeResult.ConflictingFiles = repo.Index.Conflicts.Select(i => i.Ours.Path).ToList();
                    Logger.LogInformation("Merge simulation of {sourceBranchName} into {destinationBranchName} for repository at {repoPath} resulted in conflicts in the following files: {files}.", sourceBranchName, destinationBranchName, workingDirectory, string.Join(", ", simulatedMergeResult.ConflictingFiles));
                }
                else
                {
                    simulatedMergeResult.IsSuccessful = true;
                    Logger.LogInformation("Merge simulation of {sourceBranchName} into {destinationBranchName} for repository at {repoPath} was successful with merge status: {mergeStatus}.", sourceBranchName, destinationBranchName, workingDirectory, mergeResult.Status);
                }

                dryRunResults.Add(simulatedMergeResult);
                if (!GitBranchService.TryCheckoutBranch(workingDirectory, currentBranch, out string error))
                {
                    Logger.LogError("Failed to checkout {source} after merge simulation. Reason: {reason}", currentBranch, error);
                    continue;
                }

                repo.Reset(ResetMode.Hard, originalCommit);
                if (!GitStashService.TryPopStash(workingDirectory, out string popError))
                {
                    Logger.LogError("Failed to pop stash after simulating merge of {sourceBranchName} into {destinationBranchName} for repository at {repoPath}. Error: {error}.", sourceBranchName, destinationBranchName, workingDirectory, popError);
                }
                else
                {
                    Logger.LogInformation("Successfully popped stash after simulating merge of {sourceBranchName} into {destinationBranchName} for repository at {repoPath}.", sourceBranchName, destinationBranchName, workingDirectory);
                }
            }

            return dryRunResults;
        }

        #region Private Methods

        /// <summary>
        /// Gets the user-friendly message to be returned to the client based on the merge status.
        /// </summary>
        /// <param name="mergeStatus">The merge status.</param>
        /// <param name="sourceBranch">The source branch.</param>
        /// <param name="destinationBranch">The destination branch.</param>
        /// <param name="repoName">The repository name.</param>
        /// <returns>The merge status message</returns>
        private static string GetMergeStatusMessage(MergeStatus mergeStatus, string sourceBranch, string destinationBranch, string repoName)
        {
            string branchPhrase = $"{repoName}: {sourceBranch} → {destinationBranch}";
            return mergeStatus switch
            {
                MergeStatus.Conflicts => $"{branchPhrase} Merge failed due to conflicts. Resolve conflicts and try merging again.",
                MergeStatus.UpToDate => $"{branchPhrase} Branches are already up to date. No merge necessary.",
                MergeStatus.FastForward => $"{branchPhrase} Merge successful with a fast-forward.",
                MergeStatus.NonFastForward => $"{branchPhrase} Merge successful with a non-fast-forward.",
                _ => "An unknown merge status was encountered."
            };
        }

        #endregion
    }
}
