using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing the components of response to staging/unstaging multiple files.
    /// </summary>
    public class ApplyBulkStagingActionResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the status elements.
        /// </summary>
        public List<RepositoryStatusElement> StatusElements { get; set; } = new();
    }
}
