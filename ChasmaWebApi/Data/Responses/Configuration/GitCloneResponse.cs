using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the response to cloning multiple git repositories.
    /// </summary>
    public class GitCloneResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of local git repositories that were successfully cloned.
        /// </summary>
        public List<LocalGitRepository> Repositories { get; set; } = [];

        /// <summary>
        /// Gets or sets the results of the addition of Git repositories, indicating success or failure for each repository along with any relevant reasons for failure.
        /// </summary>
        public List<RepositoryAdditionResult> AdditionResults { get; set; } = [];
    }
}
