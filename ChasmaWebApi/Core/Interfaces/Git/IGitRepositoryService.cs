using ChasmaWebApi.Data.Objects.Git;
using LibGit2Sharp;

namespace ChasmaWebApi.Core.Interfaces.Git
{
    /// <summary>
    /// Interface containing the members on the Git repository service, which is responsible for handling Git repository-level operations such as fetching branches and commits from a repository.
    /// </summary>
    public interface IGitRepositoryService
    {
        /// <summary>
        /// Gets the status of the specified repository.
        /// </summary>
        /// <param name="repoKey">The repository identifier.</param>
        /// <param name="username">The git username.</param>
        /// <param name="token">The git API token.</param>
        /// <returns>A repository summary of running the command 'git status'.</returns>
        RepositorySummary? GetRepositoryStatus(string repoKey, string username, string token);

        /// <summary>
        /// Applies the staging action to the specified files.
        /// </summary>
        /// <param name="repoId">The repository identifier.</param>
        /// <param name="fileNames">The files to be staged.</param>
        /// <param name="stagingFile">Flag indicating whether the files are being staged/unstaged.</param>
        /// <returns>The updatd repository elements.</returns>
        List<RepositoryStatusElement>? ApplyBulkStagingAction(string repoId, IEnumerable<string> fileNames, bool stagingFile);

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
        /// Tries to apply the staging operation for the patch.
        /// </summary>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="file">The file to apply the staging operation on.</param>
        /// <param name="startLine">The begin line to begin staging operation.</param>
        /// <param name="endLine">The end line to begin staging operation.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if patch was successfully applied; false otherwise.</returns>
        bool TryStagingPatch(string workingDirectory, RepositoryStatusElement file, int startLine, int endLine, out string errorMessage);
    }
}
