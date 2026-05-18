using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing a response after adding a Git repository.
    /// </summary>
    public class AddGitRepositoriesResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly added Git repositories added to the system.
        /// </summary>
        public List<LocalGitRepository> Repositories { get; set; } = new();

        /// <summary>
        /// Gets or sets the results of the addition of Git repositories, indicating success or failure for each repository along with any relevant reasons for failure.
        /// </summary>
        public List<RepositoryAdditionResult> AdditionResults { get; set; } = new();
    }
}
