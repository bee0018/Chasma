using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the response to an add local repositories request.
    /// </summary>
    public class AddLocalRepositoriesResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of current repositories.
        /// </summary>
        public List<LocalGitRepository> CurrentRepositories { get; set; }
    }
}
