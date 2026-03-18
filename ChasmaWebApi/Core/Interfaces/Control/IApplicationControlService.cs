using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.DryRun;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Objects.Shell;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Interfaces.Control
{
    /// <summary>
    /// Interface containing the members on the application control service, which is responsible for handling application-level operations.
    /// </summary>
    public interface IApplicationControlService
    {
        /// <summary>
        /// Executes the list of git command in the system shell.
        /// </summary>
        /// <param name="workingDirectory">The working directory to execute the commands in.</param>
        /// <param name="shellCommands">The shell commands to execute.</param>
        /// <returns>The list of output results from the executed commands.</returns>
        List<ShellCommandResult> RunShellCommands(string workingDirectory, IEnumerable<string> shellCommands);

        /// <summary>
        /// Executes the list of shell commands in batch for multiple working directories.
        /// </summary>
        /// <param name="entries">The entries to execute batch commands for.</param>
        /// <returns>The results of the batch commands.</returns>
        List<BatchCommandEntryResult> RunBatchShellCommands(IEnumerable<BatchCommandEntry> entries);

        /// <summary>
        /// Adds the local git repositories on the local machine and adds them to the database.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="newRepositories">The local validated local git repositories found on the system.</param>
        /// <returns>True if the repositories are found and added without error; false otherwise.</returns>
        bool TryAddLocalGitRepositoriesFromFileSystem(int userId, out List<LocalGitRepository> newRepositories);

        /// <summary>
        /// Tries to delete the repository from cache and database.
        /// </summary>
        /// <param name="repositoryId">The repository identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="localGitRepositories">The repositories that the user still has.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the repository and its resources have been deleted; false otherwise.</returns>
        bool TryDeleteRepository(string repositoryId, int userId, out List<LocalGitRepository> localGitRepositories, out string errorMessage);

        /// <summary>
        /// Tries to add a git repository to the cache with the specified filepath.
        /// </summary>
        /// <param name="repoPath">The filepath to the repository.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="localGitRepository">The add git repository.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the repository was added to the system; false otherwise.</returns>
        bool TryAddSpecificGitRepository(string repoPath, int userId, out LocalGitRepository localGitRepository, out string errorMessage);

        /// <summary>
        /// Tries to add a new branch with the specified name to the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="branchName">The branch name to be added.</param>
        /// <param name="username">The username for authentication to the repository.</param>
        /// <param name="token">The token for authentication to the repository.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch was created; false otherwise.</returns>
        bool TryAddNewBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage);

        /// <summary>
        /// Trieds to delete a branch from the specified repository.
        /// </summary>
        /// <param name="repositoryId">The repository identifier.</param>
        /// <param name="branchName">The friendly branch name.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch is successfully deleted; false otherwise.</returns>
        bool TryDeleteExistingBranch(string repositoryId, string branchName, out string errorMessage);

        /// <summary>
        /// Tries to checkout the specified branch in the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="branchName">The branch to checkout.</param>
        /// <param name="errorMessage">The error message if there an issue checking out the branch.</param>
        /// <returns>True if the branch is checked out; false otherwise.</returns>
        bool TryCheckoutBranch(string workingDirectory, string branchName, out string errorMessage);

        /// <summary>
        /// Gets the list of all branches in the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <returns>List of all local and remote branches in the repository.</returns>
        List<string> GetAllBranchesForRepository(string workingDirectory);

        /// <summary>
        /// Tries to merge the specified source branch into the current branch.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="sourceBranchName">The name of the branch to merge from.</param>
        /// <param name="destinationBranchName">The name of the branch to merge into.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the merge was successful; false otherwise.</returns>
        bool TryMergeChanges(string workingDirectory, string sourceBranchName, string destinationBranchName, string fullName, string email, string token, out string errorMessage);

        /// <summary>
        /// Gets the status of the specified repository.
        /// </summary>
        /// <param name="repoKey">The repository identifier.</param>
        /// <param name="username">The git username.</param>
        /// <param name="token">The git API token.</param>
        /// <returns>A repository summary of running the command 'git status'.</returns>
        RepositorySummary? GetRepositoryStatus(string repoKey, string username, string token);

        /// <summary>
        /// Stages or unstages the file for the specified repository.
        /// </summary>
        /// <param name="repoKey">The repository key.</param>
        /// <param name="fileName">The name of the file to stage.</param>
        /// <param name="isStaging">Flag indicating whether the file is being staged.</param>
        /// <param name="username">The git username.</param>
        /// <param name="token">The git API token.</param>
        /// <returns>A list of the updated file statuses after staging or unstaging.</returns>
        List<RepositoryStatusElement>? ApplyStagingAction(string repoKey, string fileName, bool isStaging, string username, string token);

        /// <summary>
        /// Commits the staged changes for the specified repository.
        /// </summary>
        /// <param name="filePath">The working directory of the repository.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="commitMessage">The message description of the commit.</param>
        void CommitChanges(string filePath, string fullName, string email, string commitMessage);

        /// <summary>
        /// Tries to push the committed changes to the remote repository.
        /// </summary>
        /// <param name="filePath">The filepath to the specified repository.</param>
        /// <param name="token">The git API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the user was able to push changes; false otherwise.</returns>
        bool TryPushChanges(string filePath, string token, out string errorMessage);

        /// <summary>
        /// Tries to pull changes from the remote repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="fullName">The user's full name.</param>
        /// <param name="email">The user's email.</param>
        /// <param name="token">The git API token.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the user was able to pull changes, false otherwise.</returns>
        bool TryPullChanges(string workingDirectory, string fullName, string email, string token, out string errorMessage);

        /// <summary>
        /// Tries to reset the repository to the specified revision with the given reset mode.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="revParseSpec">A revparse spec for the target commit object.</param>
        /// <param name="resetMode">Specifies the kind of operation that the repository reset should perform. </param>
        /// <param name="commitMessage">The revision short message.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the reset was successful; false otherwise.</returns>
        bool TryResetRepository(string workingDirectory, string revParseSpec, ResetMode resetMode, out string commitMessage, out string errorMessage);

        /// <summary>
        /// Tries to get the Git diff for the specified file in the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of file to get the diff of.</param>
        /// <param name="filePath">The path to the file to be diffed.</param>
        /// <param name="isStaged">Flag indicating whether to get the diff for the staged version of the file.</param>
        /// <param name="diffContent">The content as a result of the git diff operation.</param>
        /// <param name="errorMessage">The error message if an error occurs.</param>
        /// <returns>True if the file was successfully diffed; false otherwise.</returns>
        bool TryGetGitDiff(string workingDirectory, string filePath, bool isStaged, out string diffContent, out string errorMessage);

        /// <summary>
        /// Tries to add a stash to the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="user">The specified user.</param>
        /// <param name="stashMessage">The message to mark the stash.</param>
        /// <param name="stashOptions">The stash options.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the changes were stashed successfully; false otherwise.</returns>
        bool TryAddStash(string workingDirectory, UserAccountModel user, string stashMessage, StashModifiers stashOptions, out string errorMessage);

        /// <summary>
        /// Gets the stash list for the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The list of stash entries of the repository.</returns>
        List<StashEntry>? GetStashList(string workingDirectory, out string errorMessage);

        /// <summary>
        /// Gets the stash details, including the diff of the stashed changes, for the specified stash index.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashEntry">The stash entry.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The list of patch entries that are included in the stash.</returns>
        List<PatchEntry>? GetStashDetails(string workingDirectory, StashEntry stashEntry, out string errorMessage);

        /// <summary>
        /// Tries to apply the stash with the specified index to the repository.
        /// Note: The stash will be applied with the provided options, such as whether to keep the stash after applying or not.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashIndex">The stash index.</param>
        /// <param name="stashApplyOptions">The options for stashing operations.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the stash was applied; false otherwise.</returns>
        bool TryApplyStash(string workingDirectory, int stashIndex, StashApplyModifiers stashApplyOptions, out string errorMessage);

        /// <summary>
        /// Tries to remove the stash with the specified index from the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="stashIndex">The stash index.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the stash was removed; false otherwise.</returns>
        bool TryRemoveStash(string workingDirectory, int stashIndex, out string errorMessage);

        /// <summary>
        /// Tries to get the workflow run results for the repo with specified details.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <param name="repoOwner">The repository owner.</param>
        /// <param name="token">The repository access token.</param>
        /// <param name="buildCount">The threshold representing the max number of runs to retrieve.</param>
        /// <param name="workflowRunResults">The list of workflow run results.</param>
        /// <param name="errorMessage">The error message if there was a failure to retrieve runs.</param>
        /// <returns>True if the workflow runs were retrieved; false otherwise.</returns>
        bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount,
            out List<WorkflowRunResult> workflowRunResults, out string errorMessage);

        /// <summary>
        /// Tries to create a pull request in the specified repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository name.</param>
        /// <param name="title">The name of the pull request.</param>
        /// <param name="headBranch">The working branch to be merged into another.</param>
        /// <param name="baseBranch">The branch that will be getting a branch merged into.</param>
        /// <param name="body">The message of the pull request.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="pullRequestId">The identtifier of the pull request.</param>
        /// <param name="prUrl">The pull request url.</param>
        /// <param name="timestamp">The timestamp of when the pull request was created.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the pull request is created; false otherwise.</returns>
        bool TryCreatePullRequest(string workingDirectory, string owner, string repoName, string title, string headBranch, string baseBranch, string body, string token, out int pullRequestId, out string prUrl, out string timestamp, out string errorMessage);

        /// <summary>
        /// Tries to create a GitHub issue in the specified repository.
        /// </summary>
        /// <param name="repoName">The repository name.</param>
        /// <param name="repoOwner">The repository owner.</param>
        /// <param name="title">The issue title.</param>
        /// <param name="body">The issue body description.</param>
        /// <param name="token">The GitHub API token.</param>
        /// <param name="issueId">The issue identifier.</param>
        /// <param name="issueUrl">The issue URL.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the issue is created; false otherwise.</returns>
        bool TryCreateIssue(string repoName, string repoOwner, string title, string body, string token, out int issueId, out string issueUrl, out string errorMessage);

        /// <summary>
        /// Performs a dry run of the git pull operation for the specified repositories.
        /// </summary>
        /// <param name="entries">The simulation inputs.</param>
        /// <returns>The list of simulated git pull dry run results.</returns>
        List<SimulatedGitPullResult> PerformGitPullDryRun(IEnumerable<PullSimulationEntry> entries);

        /// <summary>
        /// Performs a dry run of adding a new branch to the specified repositories.
        /// </summary>
        /// <param name="entries">The simulation inputs.</param>
        /// <returns>The list of simulated branch addition results.</returns>
        List<SimulatedAddBranchResult> PerformAddBranchDryRun(IEnumerable<AddBranchSimulationEntry> entries);

        /// <summary>
        /// Performs a dry run of merging a source branch into a destination branch for the specified repositories.
        /// </summary>
        /// <param name="entries">The repository entries.</param>
        /// <returns>The list of simulated merge results.</returns>
        List<SimulatedMergeResult> PerformMergeBranchDryRun(IEnumerable<MergeSimulationEntry> entries);

        /// <summary>
        /// Tries to get the pipeline build results.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="cachedRepo">The repository in cache.</param>
        /// <param name="buildResults">The build results.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The list of pipeline job results for the specified repository.</returns>
        public bool TryGetPipelineJobResults(string workingDirectory, LocalGitRepository cachedRepo, out List<WorkflowRunResult> buildResults, out string errorMessage);
    }
}
