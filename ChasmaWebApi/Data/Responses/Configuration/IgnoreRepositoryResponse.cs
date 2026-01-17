using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the message containing the ignored repositories.
    /// </summary>
    public class IgnoreRepositoryResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of included repositories for the specified user.
        /// </summary>
        public List<LocalGitRepository> IncludedRepositories { get; set; } = new();
    }
}
