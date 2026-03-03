namespace ChasmaWebApi.Core.Interfaces.Git
{
    /// <summary>
    /// Interface containing the members on the Git branch service, which is responsible for handling Git branch-level operations such as fetching branches and commits from a repository.
    /// </summary>
    public interface IGitBranchService
    {
        /// <summary>
        /// Tries to add a branch with the specified name to the repository.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <param name="branchName">The branch name to be added.</param>
        /// <param name="username">The username for authentication to the repository.</param>
        /// <param name="token">The token for authentication to the repository.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch was created; false otherwise.</returns>
        bool TryAddBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage);

        /// <summary>
        /// Trieds to delete a branch from the specified repository.
        /// </summary>
        /// <param name="repositoryId">The repository identifier.</param>
        /// <param name="branchName">The friendly branch name.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the branch is successfully deleted; false otherwise.</returns>
        bool TryDeleteBranch(string repositoryId, string branchName, out string errorMessage);

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
        List<string> GetAllBranches(string workingDirectory);

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
        bool TryMergeBranch(string workingDirectory, string sourceBranchName, string destinationBranchName, string fullName, string email, string token, out string errorMessage);
    }
}
