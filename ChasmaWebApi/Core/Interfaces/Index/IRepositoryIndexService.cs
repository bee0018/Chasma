using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Core.Interfaces.Index
{
    /// <summary>
    /// Interface containing the members on the repository index service, which is responsible for handling repository-level operations such as adding and deleting repositories from the system.
    /// </summary>
    public interface IRepositoryIndexService
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
        /// Adds a git repositories to the cache with the specified filepath.
        /// </summary>
        /// <param name="repoPaths">The list of filepaths to its repository.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="newRepositories">The added git repositories.</param>
        /// <returns>The list of repository addition results.</returns>
        List<RepositoryAdditionResult> AddGitRepositories(IEnumerable<string> repoPaths, int userId, out List<NewRepository> newRepositories);

        /// <summary>
        /// Tries to remove the file from the repository index and stage the deletion for the next commit.
        /// </summary>
        /// <param name="selectedFile">The file to delete.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>True if the file is removed; false otherwise.</returns>
        bool TryRemoveFile(RepositoryStatusElement selectedFile, out string errorMessage);

        /// <summary>
        /// Clones git repositories to the cache with the specified filepath.
        /// </summary>
        /// <param name="blueprints">The list of blueprints for git cloning.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="newRepositories">The clone git repositories.</param>
        /// <returns>The list of repository addition results.</returns>
        List<RepositoryAdditionResult> CloneRepositories(IEnumerable<GitCloneBlueprint> blueprints, int userId, out List<NewRepository> newRepositories);
    }
}
