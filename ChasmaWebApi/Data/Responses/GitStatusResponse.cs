using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the components of a Git Status response.
    /// </summary>
    public class GitStatusResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the results of the 'git status' command for the repository.
        /// </summary>
        public List<RepositoryStatusElement> StatusElements { get; set; } = new();
    }
}
