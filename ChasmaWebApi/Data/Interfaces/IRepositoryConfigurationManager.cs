using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Interfaces;

/// <summary>
/// Interface containing the members on the GitHub Workflow Manager.
/// </summary>
public interface IRepositoryConfigurationManager
{
    /// <summary>
    /// Adds the local git repositories on the local machine and adds them to the database.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="newRepositories">The local validated local git repositories found on the system.</param>
    /// <returns>True if the repositories are found and added without error; false otherwise.</returns>
    bool TryAddLocalGitRepositories(int userId, out List<LocalGitRepository> newRepositories);

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
    /// Trieds to delete a branch from the specified repository.
    /// </summary>
    /// <param name="repositoryId">The repository identifier.</param>
    /// <param name="branchName">The friendly branch name.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if the branch is successfully deleted; false otherwise.</returns>
    bool TryDeleteBranch(string repositoryId, string branchName, out string errorMessage);

    /// <summary>
    /// Tries to add a git repository to the cache with the specified filepath.
    /// </summary>
    /// <param name="repoPath">The filepath to the repository.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="localGitRepository">The add git repository.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if the repository was added to the system; false otherwise.</returns>
    bool TryAddGitRepository(string repoPath, int userId, out LocalGitRepository localGitRepository, out string errorMessage);

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
    /// Tries to add a branch with the specified name to the repository.
    /// </summary>
    /// <param name="workingDirectory">The working directory of the repository.</param>
    /// <param name="branchName">The branch name to be added.</param>
    /// <param name="username">The username for authentication to the repository.</param>
    /// <param name="token">The token for authentication to the repository.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>True if the branch was created; false otherwise.</returns>
    bool TryAddBranch(string workingDirectory, string branchName, string username, string token, out string errorMessage);
}