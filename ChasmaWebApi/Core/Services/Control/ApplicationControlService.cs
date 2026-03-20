using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Index;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Core.Interfaces.Simulation;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Objects.Shell;
using LibGit2Sharp;

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
        public ApplicationControlService(IRepositoryIndexService repoIndexService, IGitRepositoryService gitRepoService, IGitBranchService branchService, IShellExecutionService shellService, IGitStashService stashService, IGitHubService gitHubRemoteService, ISimulationService simService, IGitLabService gitlabService)
        {
            repositoryIndexService = repoIndexService;
            gitRepositoryService = gitRepoService;
            gitBranchService = branchService;
            shellExecutionService = shellService;
            gitStashService = stashService;
            gitHubService = gitHubRemoteService;
            simulationService = simService;
            gitLabService = gitlabService;
        }

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

        #endregion

        #region Repository Configuration

        // <inheritdoc />
        public bool TryAddLocalGitRepositoriesFromFileSystem(int userId, out List<LocalGitRepository> newRepositories)
        {
            return repositoryIndexService.TryAddLocalGitRepositories(userId, out newRepositories);
        }

        // <inheritdoc />
        public bool TryAddSpecificGitRepository(string repoPath, int userId, out LocalGitRepository localGitRepository, out string errorMessage)
        {
            return repositoryIndexService.TryAddGitRepository(repoPath, userId, out localGitRepository, out errorMessage);
        }

        // <inheritdoc />
        public bool TryDeleteRepository(string repositoryId, int userId, out List<LocalGitRepository> localGitRepositories, out string errorMessage)
        {
            return repositoryIndexService.TryDeleteRepository(repositoryId, userId, out localGitRepositories, out errorMessage);
        }

        #endregion

        #region Branch Configuration

        // <inheritdoc />
        public bool TryAddNewBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage)
        {
            return gitBranchService.TryAddBranch(workingDirectory, branchName, username, token, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCheckoutBranch(string workingDirectory, string branchName, out string errorMessage)
        {
            return gitBranchService.TryCheckoutBranch(workingDirectory, branchName, out errorMessage);
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
        public bool TryMergeChanges(string workingDirectory, string sourceBranchName, string destinationBranchName, string fullName, string email, string token, out string errorMessage)
        {
            return gitBranchService.TryMergeBranch(workingDirectory, sourceBranchName, destinationBranchName, fullName, email, token, out errorMessage);
        }

        #endregion

        #region Repository Status Interactions 

        // <inheritdoc />
        public RepositorySummary? GetRepositoryStatus(string repoKey, string username, string token)
        {
            return gitRepositoryService.GetRepositoryStatus(repoKey, username, token);
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
        public bool TryPushChanges(string filePath, string token, out string errorMessage)
        {
            return gitRepositoryService.TryPushChanges(filePath, token, out errorMessage);
        }

        // <inheritdoc />
        public bool TryPullChanges(string workingDirectory, string fullName, string email, string token, out string errorMessage)
        {
            return gitRepositoryService.TryPullChanges(workingDirectory, fullName, email, token, out errorMessage);
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

        #endregion

        #region Stash Functionality

        // <inheritdoc />
        public bool TryAddStash(string workingDirectory, UserAccountModel user, string stashMessage, StashModifiers stashOptions, out string errorMessage)
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
        public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
        {
            return gitHubService.TryGetWorkflowRunResults(repoName, repoOwner, token, buildCount, out workflowRunResults, out errorMessage);
        }

        // <inheritdoc />
        public bool TryCreatePullRequest(string workingDirectory, string owner, string repoName, string title, string headBranch, string baseBranch, string body, string token, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage)
        {
            return gitHubService.TryCreatePullRequest(workingDirectory, owner, repoName, title, headBranch, baseBranch, body, token, out pullRequestId, out prUrl, out timestamp, out errorMessage);
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
    }
}
