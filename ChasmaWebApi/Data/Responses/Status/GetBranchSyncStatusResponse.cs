using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing the response to the getting the branch synchronization status.
    /// </summary>
    public class GetBranchSyncStatusResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of branch synchronization statuses.
        /// </summary>
        public List<BranchSyncStatus> BranchSyncStatuses { get; set; } = new();
    }
}
