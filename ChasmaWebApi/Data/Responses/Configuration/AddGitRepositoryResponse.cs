using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing a response after adding a Git repository.
    /// </summary>
    public class AddGitRepositoryResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly added Git repository added to the system.
        /// </summary>
        public LocalGitRepository Repository { get; set; }
    }
}
