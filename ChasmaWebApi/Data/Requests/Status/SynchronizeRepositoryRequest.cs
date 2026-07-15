using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Status;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing a request to synchronize the state in a git repository.
    /// </summary>
    public class SynchronizeRepositoryRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the repository identifier for the repository in which branches should be pruned.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the synchronization step that should be performed on the repository.
        /// </summary>
        public SynchronizationStep SyncStep { get; set; }

        /// <summary>
        /// Gets or sets the branch checkout mode to determine how to handle uncommitted changes when checking out a branch.
        /// </summary>
        public BranchCheckoutMode CheckoutMode { get; set; }
    }
}
