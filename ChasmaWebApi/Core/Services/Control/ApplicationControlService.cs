using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Index;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Core.Interfaces.Simulation;
using ChasmaWebApi.Core.Services.Git;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Objects.Shell;
using ChasmaWebApi.Data.Objects.Status;
using ChasmaWebApi.Util;
using LibGit2Sharp;
using System.Diagnostics;
using System.Text;

namespace ChasmaWebApi.Core.Services.Control
{
    /// <summary>
    /// Service class containing the implementation of the members on the application control service, which is responsible for handling application-level operations.
    /// </summary>
    public class ApplicationControlService : IApplicationControlService
    {
        /// <summary>
        /// The repository index service, which is responsible for handling repository-level operations such as adding and deleting repositories from the system.
        /// </summary>
        private readonly IRepositoryIndexService repositoryIndexService;

        /// <summary>
        /// The Git repository service, which is responsible for handling Git repository-level operations such as fetching branches and commits from a repository.
        /// </summary>
        private readonly IGitRepositoryService gitRepositoryService;

        /// <summary>
        /// Provides access to branch-related operations for Git repositories.
        /// </summary>
        private readonly IGitBranchService gitBranchService;

        /// <summary>
        /// Provides methods for executing shell commands, both individually and in batches, and handling their results.
        /// </summary>
        private readonly IShellExecutionService shellExecutionService;

        /// <summary>
        /// The Git stash service, which is responsible for handling Git stash operations such as creating, applying, and deleting stashes.
        /// </summary>
        private readonly IGitStashService gitStashService;

        /// <summary>
        /// The GitHub service, which is responsible for handling interactions with the GitHub API for operations such as fetching repository information and managing pull requests.
        /// </summary>
        private readonly IGitHubService gitHubService;

        /// <summary>
        /// The simulation service used for dry running git operations.
        /// </summary>
        private readonly ISimulationService simulationService;

        /// <summary>
        /// The GitLab service, responsible for interacting with the GitLab API.
        /// </summary>
        private readonly IGitLabService gitLabService;

        /// <summary>
        /// The logging instance for this class.
        /// </summary>
        private readonly ILogger<ApplicationControlService> logger;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The encryption service for managing credentials data.
        /// </summary>
        private readonly IEncryptionService encryptionService;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationControlService"/> class with the specified dependencies.
        /// </summary>
        /// <param name="repoIndexService">The repository index service.</param>
        /// <param name="gitRepoService">The repository status service.</param>
        /// <param name="branchService">The branch management service.</param>
        /// <param name="shellService">The shell service.</param>
        /// <param name="stashService">The stash management service.</param>
        /// <param name="gitHubRemoteService">The GitHub remote repository management service.</param>
        /// <param name="simService">The git operation simulation service.</param>
        /// <param name="gitlabService">The GitLab remote repository management service.</param>
        /// <param name="log">The internal logging instance.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        /// <param name="apiEncryptionService">The internal encryption service.</param>
        public ApplicationControlService(
            IRepositoryIndexService repoIndexService,
            IGitRepositoryService gitRepoService,
            IGitBranchService branchService,
            IShellExecutionService shellService,
            IGitStashService stashService,
            IGitHubService gitHubRemoteService,
            ISimulationService simService,
            IGitLabService gitlabService,
            ILogger<ApplicationControlService> log,
            ICacheManager apiCacheManager,
            IEncryptionService apiEncryptionService)
        {
            repositoryIndexService = repoIndexService;
            gitRepositoryService = gitRepoService;
            gitBranchService = branchService;
            shellExecutionService = shellService;
            gitStashService = stashService;
            gitHubService = gitHubRemoteService;
            simulationService = simService;
            gitLabService = gitlabService;
            logger = log;
            cacheManager = apiCacheManager;
            encryptionService = apiEncryptionService;
        }

        #endregion

        #region Infrastructure

        // <inheritdoc />
        public void UpdateApiConfiguration(string configFilePath, ChasmaWebApiConfigurations newConfig, ChasmaWebApiConfigurations currentConfig)
        {
            currentConfig.Update(newConfig, encryptionService);
            string xmlText = ChasmaXmlBase.GenerateXml(currentConfig);
            File.WriteAllText(configFilePath, xmlText, Encoding.UTF8);
        }

        // <inheritdoc />
        public bool TryApplyUpdateAndRestartApplication(SystemManifest systemManifest, bool isDevelopmentMode, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                string buildArtifactPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Emryce", "Updates", systemManifest.Version);
                IEnumerable<string> zipFiles;
                EnumerationOptions options = new()
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = false
                };
                if (OperatingSystem.IsWindows())
                {
                    zipFiles = Directory.EnumerateFiles(buildArtifactPath, "*.zip", options);
                }
                else if (OperatingSystem.IsLinux())
                {
                    zipFiles = Directory.EnumerateFiles(buildArtifactPath, "*.tar", options);
                }
                else
                {
                    errorMessage = "OS is not supported for deploying updates";
                    logger.LogError("{error}. Sending error response.", errorMessage);
                    return false;
                }

                string zipFile = zipFiles.FirstOrDefault();
                if (string.IsNullOrEmpty(zipFile))
                {
                    errorMessage = $"No download build artifacts could be found. Cannot deploy update for version {systemManifest.Version}";
                    logger.LogError("{error}. Sending error response.", errorMessage);
                    return false;
                }

                string currentProcessFilepath = Path.GetDirectoryName(Environment.ProcessPath);
                string executablePath = Environment.ProcessPath;
                int processId = Environment.ProcessId;
                string updaterExePath = isDevelopmentMode
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmryceUpdater", "net10.0", "EmryceUpdater.exe")
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmryceUpdater.exe");
                if (!File.Exists(updaterExePath))
                {
                    errorMessage = $"No updater file cannot be found. Cannot deploy update for version {systemManifest.Version}";
                    logger.LogError("{error}. Sending error response.", errorMessage);
                    return false;
                }

                ProcessStartInfo startInfo = new()
                {
                    FileName = updaterExePath,
                    Arguments = $"\"{zipFile}\" \"{currentProcessFilepath}\" \"{executablePath}\" {processId}",
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory = Path.GetDirectoryName(updaterExePath),
                };
                Process.Start(startInfo);
                Thread.Sleep(100);
                Environment.Exit(0);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "Error when attempting to apply system updates. View server logs for more information";
                logger.LogError("Recieved the following error when trying to apply system update: {message}", e.Message);
                return false;
            }
        }

        #endregion

        #region Shell Interactions

        // <inheritdoc />
        public List<BatchCommandEntryResult> RunBatchShellCommands(IEnumerable<BatchCommandEntry> entries)
        {
            return shellExecutionService.ExecuteShellCommandsInBatch(entries);
        }

        // <inheritdoc />
        public List<ShellCommandResult> RunShellCommands(string workingDirectory, IEnumerable<string> shellCommands)
        {
            return shellExecutionService.ExecuteShellCommands(workingDirectory, shellCommands);
        }

        // <inheritdoc />
        public bool TryOpenApplicationLogs(out string errorMessage)
        {
            return shellExecutionService.TryOpenApiLogs(out errorMessage);
        }

        #endregion

        #region Repository Configuration

        // <inheritdoc />
        public List<RepositoryAdditionResult> AddGitRepositories(IEnumerable<string> repoPaths, int userId, out List<NewRepository> newRepositories)
        {
            return repositoryIndexService.AddGitRepositories(repoPaths, userId, out newRepositories);
        }

        // <inheritdoc />
        public bool TryDeleteRepository(string repositoryId, int userId, out List<LocalGitRepository> localGitRepositories, out string errorMessage)
        {
            return repositoryIndexService.TryDeleteRepository(repositoryId, userId, out localGitRepositories, out errorMessage);
        }

        // <inheritdoc />
        public bool TryDeleteFile(RepositoryStatusElement selectedFile, out string errorMessage)
        {
            return repositoryIndexService.TryRemoveFile(selectedFile, out errorMessage);
        }

        // <inheritdoc />
        public List<RepositoryAdditionResult> CloneGitRepositories(IEnumerable<GitCloneBlueprint> blueprints, int userId, out List<NewRepository> newRepositories)
        {
            return repositoryIndexService.CloneRepositories(blueprints, userId, out newRepositories);
        }

        #endregion

        #region Branch Configuration

        // <inheritdoc />
        public bool TryAddNewBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage)
        {
            return gitBranchService.TryAddBranch(workingDirectory, branchName, username, token, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCheckoutBranch(string workingDirectory, string branchName, BranchCheckoutMode checkoutMode, string? stashMessage, ApplicationUser user, out string errorMessage)
        {
            return gitBranchService.TryCheckoutBranch(workingDirectory, branchName, checkoutMode, stashMessage, out errorMessage, user);
        }

        // <inheritdoc />
        public bool TryDeleteExistingBranch(string repositoryId, string branchName, out string errorMessage)
        {
            return gitBranchService.TryDeleteBranch(repositoryId, branchName, out errorMessage);
        }

        // <inheritdoc />
        public List<string> GetAllBranchesForRepository(string workingDirectory)
        {
            return gitBranchService.GetAllBranches(workingDirectory);
        }

        // <inheritdoc />
        public bool TryMergeChanges(string workingDirectory, string sourceBranchName, string destinationBranchName, ApplicationUser user, LocalGitRepository localGitRepository, out string errorMessage)
        {
            return gitBranchService.TryMergeBranch(workingDirectory, sourceBranchName, destinationBranchName, user, localGitRepository, out errorMessage);
        }

        #endregion

        #region Repository Status Interactions 

        // <inheritdoc />
        public RepositorySummary? GetRepositoryStatus(string repoKey)
        {
            return gitRepositoryService.GetRepositoryStatus(repoKey);
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyBulkStagingAction(string repoId, IEnumerable<string> fileNames, bool stagingFile)
        {
            return gitRepositoryService.ApplyBulkStagingAction(repoId, fileNames, stagingFile);
        }

        // <inheritdoc />
        public List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool isStaging, string username, string token)
        {
            return gitRepositoryService.ApplyStagingAction(repoKey, fileName, isStaging, username, token);
        }

        // <inheritdoc />
        public void CommitChanges(string filePath, string fullName, string email, string commitMessage)
        {
            gitRepositoryService.CommitChanges(filePath, fullName, email, commitMessage);
        }

        // <inheritdoc />
        public bool TryPushChanges(string filePath, LocalGitRepository localGitRepository, out string errorMessage)
        {
            return gitRepositoryService.TryPushChanges(filePath, localGitRepository, out errorMessage);
        }

        // <inheritdoc />
        public bool TryPullChanges(string workingDirectory, ApplicationUser user, LocalGitRepository localGitRepository, out string errorMessage)
        {
            return gitRepositoryService.TryPullChanges(workingDirectory, user, localGitRepository, out errorMessage);
        }

        // <inheritdoc />
        public bool TryResetRepository(string workingDirectory, string revParseSpec, ResetMode resetMode, out string commitMessage, out string errorMessage)
        {
            return gitRepositoryService.TryResetRepository(workingDirectory, revParseSpec, resetMode, out commitMessage, out errorMessage);
        }

        // <inheritdoc />
        public bool TryGetGitDiff(string workingDirectory, string filePath, bool isStaged, out string diffContent, out string errorMessage)
        {
            return gitRepositoryService.TryGetGitDiff(workingDirectory, filePath, isStaged, out diffContent, out errorMessage);
        }

        // <inheritdoc />
        public List<BranchSyncStatus> GetBranchSyncStatuses(string branchName, bool skipBuildRetrieval, bool syncSpecifiedBranch, ApplicationUser user)
        {
            List<BranchSyncStatus> statuses = new();
            foreach (LocalGitRepository repository in cacheManager.Repositories.Values)
            {
                // We know the key to exist so this is a safe operation.
                string workingDirectory = cacheManager.WorkingDirectories[repository.Id];
                (string BuildStatus, string BuildConclusion) buildMetrics = ("N/A", "N/A");
                RepositoryHealthScore healthScore = new() { ScoreCategory = "Unknown" };
                BranchSyncStatus branchSyncStatus = new();
                string headBranchName = GitBranchService.GetHeadBranchName(workingDirectory, logger);
                string branchToSync = syncSpecifiedBranch ? branchName : headBranchName;
                if (!GitBranchService.DoesBranchExist(workingDirectory, branchToSync, logger, syncSpecifiedBranch))
                {
                    branchSyncStatus = new()
                    {
                        RepositoryName = repository.GetDisplayName(),
                        BranchName = branchToSync,
                        BranchExists = false,
                        Ahead = "-",
                        Behind = "-",
                        PullRequestOpen = false,
                        BuildStatus = buildMetrics.BuildStatus,
                        LastUpdated = "-",
                        HealthScore = healthScore,
                    };
                }
                else
                {
                    bool checkoutNeeded = (headBranchName != branchName) && syncSpecifiedBranch;
                    if (checkoutNeeded)
                    {
                        string stashMessage = $"Auto stash for branch sync status at ${DateTimeOffset.Now:g}";
                        if (!gitBranchService.TryCheckoutBranch(workingDirectory, branchToSync, BranchCheckoutMode.StashOnly, stashMessage, out string errorMessage, user))
                        {
                            healthScore.ScoreCategory = "Failed to get status";
                            branchSyncStatus = new()
                            {
                                RepositoryName = repository.GetDisplayName(),
                                BranchName = branchToSync,
                                BranchExists = false,
                                Ahead = "-",
                                Behind = "-",
                                PullRequestOpen = false,
                                BuildStatus = buildMetrics.BuildStatus,
                                LastUpdated = "-",
                                HealthScore = healthScore,
                            };
                            statuses.Add(branchSyncStatus);
                            continue;
                        }
                    }

                    RemoteHostPlatform remoteHostPlatform = repository.HostPlatform;
                    string token = RemoteHelper.GetApiToken(remoteHostPlatform);
                    string decryptedToken = encryptionService.DecryptString(token);
                    string username = RemoteHelper.GetRemoteHostUsername(repository);
                    bool isPullRequestOpen = false;
                    if (!skipBuildRetrieval)
                    {
                        string repoName = repository.Name;
                        string repoOwner = repository.Owner;
                        if (string.IsNullOrEmpty(decryptedToken))
                        {
                            logger.LogWarning("No API token found for repository {repoName} with remote host platform {remoteHostPlatform}. Unable to fetch build status from remote host platform.", repository.GetDisplayName(), remoteHostPlatform);
                        }
                        else if (remoteHostPlatform == RemoteHostPlatform.GitHub)
                        {
                            isPullRequestOpen = cacheManager.GitHubPullRequests.Values.Any(i => i.RepositoryId == repository.Id && i.BranchName == branchToSync && !i.Merged);
                            if (gitHubService.TryGetWorkflowRunResults(repoName, repoOwner, decryptedToken, out List<WorkflowRunResult> gitHubResults, out _))
                            {
                                buildMetrics = GetBuildStatusFromRemoteBuildResults(gitHubResults, branchToSync);
                            }
                        }
                        else if (remoteHostPlatform == RemoteHostPlatform.GitLab)
                        {
                            isPullRequestOpen = cacheManager.GitLabMergeRequests.Values.Any(i => i.RepositoryId == repository.Id && i.BranchName == branchToSync && !i.Merged);
                            if (gitLabService.TryGetPipelineJobResults(repository, out List<WorkflowRunResult> gitLabResults, out _))
                            {
                                buildMetrics = GetBuildStatusFromRemoteBuildResults(gitLabResults, branchToSync);
                            }
                        }
                    }

                    RepositorySummary? repositorySummary = gitRepositoryService.GetRepositoryStatus(repository.Id);
                    healthScore = gitRepositoryService.GetHealthScore(buildMetrics.BuildConclusion, repositorySummary, repository);
                    branchSyncStatus = new()
                    {
                        RepositoryName = repository.GetDisplayName(),
                        BranchName = branchToSync,
                        BranchExists = true,
                        Ahead = repositorySummary?.CommitsAhead.ToString() ?? "",
                        Behind = repositorySummary?.CommitsBehind.ToString() ?? "",
                        PullRequestOpen = isPullRequestOpen,
                        BuildStatus = buildMetrics.BuildStatus,
                        LastUpdated = repositorySummary?.LastUpdated ?? "",
                        HealthScore = healthScore,
                    };
                    if (checkoutNeeded)
                    {
                        if (!gitBranchService.TryCheckoutBranch(workingDirectory, headBranchName, BranchCheckoutMode.Default, null, out string errorMessage, user))
                        {
                            logger.LogError("Failed to checkout original branch {branch} for repository {repo}: {error}", headBranchName, repository.GetDisplayName(), errorMessage);
                        }

                        if (!gitStashService.TryPopStash(workingDirectory, out string stashApplyError))
                        {
                            logger.LogError("Failed to apply stash on original branch {branch} for repository {repo}: {error}", headBranchName, repository.GetDisplayName(), stashApplyError);
                        }
                    }
                }

                statuses.Add(branchSyncStatus);
            }

            return statuses;
        }

        // <inheritdoc />
        public bool TryRestoringFile(RepositoryStatusElement selectedFile, out string errorMessage)
        {
            return gitRepositoryService.TryGitRestore(selectedFile, out errorMessage);
        }

        // <inheritdoc />
        public List<RepositorySnapshotAdditionResult> AddWorkContextSnapshot(int userId, string snapshotDisplayName, IEnumerable<RepositorySnapshotBlueprint> blueprints, string? snapshotNote, out WorkContextSnapshot snapshot)
        {
            return gitRepositoryService.AddWorkContextSnapshot(userId, snapshotDisplayName, blueprints, snapshotNote, out snapshot);
        }

        // <inheritdoc />
        public List<RepositorySnapshotAdditionResult> ApplyWorkspaceContextSnapshot(WorkContextSnapshot snapshot)
        {
            return gitRepositoryService.LoadWorkspaceContextSnapshot(snapshot);
        }

        // <inheritdoc />
        public bool TryPerformSynchronizationStep(string workingDirectory, LocalGitRepository repository, SynchronizationStep syncStep, BranchCheckoutMode branchCheckoutMode, ApplicationUser user, out string executionOutput)
        {
            if (syncStep == SynchronizationStep.PreFlightChecks)
            {
                int numberOfPrunedBranches = gitBranchService.TryPruneBranches(workingDirectory, repository, out string errorMessage);
                if (numberOfPrunedBranches == -1)
                {
                    executionOutput = $"Ran into errors when attempting to prune branches in {repository.GetDisplayName()}: {errorMessage}";
                    return false;
                }

                executionOutput = $"Pre-flight checks completed. {numberOfPrunedBranches} branches were pruned.";
                return true;
            }

            if (syncStep == SynchronizationStep.PullChanges)
            {
                RepositorySummary? repositoryMetrics = gitRepositoryService.GetRepositoryStatus(repository.Id);
                if (repositoryMetrics == null)
                {
                    executionOutput = $"Unable to retrieve repository metrics in {repository.GetDisplayName()}. Cannot perform pull changes.";
                    return false;
                }

                int commitsBehind = repositoryMetrics.CommitsBehind;
                if (commitsBehind == 0)
                {
                    executionOutput = "No changes to pull.";
                    return true;
                }

                string stashMessage = $"Auto-stash before pulling changes in {repository.GetDisplayName()} on branch {repositoryMetrics.BranchName} at {DateTimeOffset.Now.ToLocalTime():g}";
                if (!gitBranchService.TryHandleWorkingDirectoryChanges(workingDirectory, repositoryMetrics.BranchName, branchCheckoutMode, stashMessage, out string fileHandlingError, user))
                {
                    logger.LogError("Error occurred while handling working directory changes in {repositoryName}: {errorMessage}", repository.GetDisplayName(), fileHandlingError);
                    executionOutput = $"{fileHandlingError}";
                    return false;
                }

                bool manifestFileExists = gitRepositoryService.AreManifestFilesInChangeset(workingDirectory);
                if (!gitRepositoryService.TryPullChanges(workingDirectory, user, repository, out string errorMessage))
                {
                    executionOutput = $"Error occurred while pulling changes in {repository.GetDisplayName()}: {errorMessage}";
                    return false;
                }

                if (branchCheckoutMode == BranchCheckoutMode.KeepChanges && !gitStashService.TryPopStash(workingDirectory, out string stashApplyError))
                {
                    executionOutput = $"Error occurred while applying stashed changes in {repository.GetDisplayName()}: {stashApplyError}";
                    return false;
                }

                string commitPhrase = commitsBehind == 1 ? "commit" : "commits";
                executionOutput = manifestFileExists
                    ? $"Successfully pulled in {commitsBehind} {commitPhrase}. Manifest files were detected, therefore a build or restore is recommended."
                    : $"Successfully pulled {commitsBehind} {commitPhrase}.";
                return true;
            }

            if (syncStep == SynchronizationStep.PushChanges)
            {
                RepositorySummary? repositoryMetrics = gitRepositoryService.GetRepositoryStatus(repository.Id);
                if (repositoryMetrics == null)
                {
                    executionOutput = $"Unable to retrieve repository metrics in {repository.GetDisplayName()}. Cannot perform push changes.";
                    return false;
                }

                if (repositoryMetrics.CommitsAhead == 0)
                {
                    executionOutput = "No changes to push.";
                    return true;
                }

                if (!gitRepositoryService.TryPushChanges(workingDirectory, repository, out string pushError))
                {
                    logger.LogError("Error occurred while pushing changes in {repositoryName}: {errorMessage}", repository.GetDisplayName(), pushError);
                    executionOutput = $"{pushError}";
                    return false;
                }

                executionOutput = "Pushed changes completed successfully.";
                return true;
            }

            executionOutput = "Invalid synchronization step specified.";
            return false;
        }

        #endregion

        #region Stash Functionality

        // <inheritdoc />
        public bool TryAddStash(string workingDirectory, ApplicationUser user, string stashMessage, StashModifiers stashOptions, out string errorMessage)
        {
            return gitStashService.TryAddStash(workingDirectory, user, stashMessage, stashOptions, out errorMessage);
        }

        // <inheritdoc />
        public List<StashEntry>? GetStashList(string workingDirectory, out string errorMessage)
        {
            return gitStashService.GetStashList(workingDirectory, out errorMessage);
        }

        // <inheritdoc />
        public List<PatchEntry>? GetStashDetails(string workingDirectory, StashEntry stashEntry, out string errorMessage)
        {
            return gitStashService.GetStashDetails(workingDirectory, stashEntry, out errorMessage);
        }

        // <inheritdoc />
        public bool TryApplyStash(string workingDirectory, int stashIndex, StashApplyModifiers stashApplyOptions, out string errorMessage)
        {
            return gitStashService.TryApplyStash(workingDirectory, stashIndex, stashApplyOptions, out errorMessage);
        }

        // <inheritdoc />
        public bool TryRemoveStash(string workingDirectory, int stashIndex, out string errorMessage)
        {
            return gitStashService.TryRemoveStash(workingDirectory, stashIndex, out errorMessage);
        }

        #endregion

        #region Remote Interactions - GitHub

        // <inheritdoc />
        public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
        {
            return gitHubService.TryGetWorkflowRunResults(repoName, repoOwner, token, out workflowRunResults, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCreatePullRequest(PreparedGitHubPullRequest pullRequest, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage)
        {
            return gitHubService.TryCreatePullRequest(pullRequest, out pullRequestId, out prUrl, out timestamp, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCreateIssue(string repoName, string repoOwner, string title, string body, string token, out int issueId, out string issueUrl, out string errorMessage)
        {
            return gitHubService.TryCreateIssue(repoName, repoOwner, title, body, token, out issueId, out issueUrl, out errorMessage);
        }

        #endregion

        #region Remote Interactions - GitLab

        // <inheritdoc />
        public bool TryGetPipelineJobResults(LocalGitRepository repository, out List<WorkflowRunResult> buildResults, out string errorMessage)
        {
            return gitLabService.TryGetPipelineJobResults(repository, out buildResults, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCreateIssue(PreparedGitLabIssue issueCreation, out GitLabIssueResult issue, out string errorMessage)
        {
            return gitLabService.TryCreateIssue(issueCreation, out  issue, out errorMessage);
        }

        // <inheritdoc />
        public bool TryGetMembers(LocalGitRepository repository, out List<GitLabProjectMember> members, out long projectId, out string errorMessage)
        {
            return gitLabService.TryGetUsersInProject(repository, out members, out projectId, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCreateMergeRequest(PreparedGitLabMergeRequest mergeRequest, out MergeRequestResult mergeResult, out string errorMessage)
        {
            return gitLabService.TryCreateMergeRequest(mergeRequest, out mergeResult, out errorMessage);
        }

        #endregion

        #region Dry Run Simulations

        // <inheritdoc />
        public List<SimulatedGitPullResult> PerformGitPullDryRun(IEnumerable<PullSimulationEntry> entries)
        {
            return simulationService.SimulateGitPull(entries);
        }

        // <inheritdoc />
        public List<SimulatedAddBranchResult> PerformAddBranchDryRun(IEnumerable<AddBranchSimulationEntry> entries)
        {
            return simulationService.SimulateAddBranch(entries);
        }

        // <inheritdoc />
        public List<SimulatedMergeResult> PerformMergeBranchDryRun(IEnumerable<MergeSimulationEntry> entries)
        {
            return simulationService.SimulateMergeBranch(entries);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the build status from the remote builds from the remote host platform.
        /// </summary>
        /// <param name="builds">The list of build results.</param>
        /// <param name="branchName">The branch name to search builds for.</param>
        /// <returns>The build status for the most recent specified branch name.</returns>
        private static (string BuildStatus, string BuildConclusion) GetBuildStatusFromRemoteBuildResults(IEnumerable<WorkflowRunResult> builds, string branchName)
        {
            bool buildsExistForBranch = false;
            IOrderedEnumerable<WorkflowRunResult> orderedBuilds = builds.OrderByDescending(i => DateTimeOffset.Parse(i.UpdatedDate));
            if (orderedBuilds.Any(i => i.BranchName == branchName))
            {
                buildsExistForBranch = true;
            }

            ChasmaWebApiConfigurations apiConfigurations = ChasmaWebApiConfigurations.GetApiConfig();
            WorkflowRunResult mostRecentBuild = orderedBuilds.FirstOrDefault(i => i.BranchName == branchName);
            if (buildsExistForBranch && mostRecentBuild == null)
            {
                // A build exists for this branch in the lifetime of this repository, however, it is not recent in the reported number of builds that is being sent out.
                return ("Stale", "-");
            }

            if (buildsExistForBranch && mostRecentBuild != null)
            {
                // This build is recent and up to date.
                return (mostRecentBuild.BuildStatus, mostRecentBuild.BuildConclusion);
            }

            // Builds have not been configured for this branch.
            return ("-", "-");
        }

        #endregion
    }
}
