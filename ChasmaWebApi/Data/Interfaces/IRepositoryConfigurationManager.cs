using ChasmaWebApi.Data.Objects;

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
}