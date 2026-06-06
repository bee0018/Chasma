using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Gets or sets the response for changing the display name of a repository.
    /// </summary>
    public class ChangeRepositoryDisplayNameResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the updated local git repository with the new display name.
        /// </summary>
        public LocalGitRepository Repository { get; set; }
    }
}
