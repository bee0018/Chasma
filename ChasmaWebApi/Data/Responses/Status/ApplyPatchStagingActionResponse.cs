using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing a response to apply a staging operation to a patch of data.
    /// </summary>
    public class ApplyPatchStagingActionResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of status elements in the index.
        /// </summary>
        public List<RepositoryStatusElement> StatusElements { get; set; } = new();
    }
}
