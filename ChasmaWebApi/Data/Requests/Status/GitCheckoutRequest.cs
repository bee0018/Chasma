using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing a request to perform a Git checkout operation.
    /// </summary>
    public class GitCheckoutRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the ID of the repository to perform the checkout on.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user performing the checkout operation.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the branch to checkout.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the branch checkout mode to determine how to handle uncommitted changes when checking out a branch.
        /// </summary>
        public BranchCheckoutMode CheckoutMode { get; set; }

        /// <summary>
        /// Gets or sets the stash message to use if stashing is necessary during the checkouit process.
        /// </summary>
        public string? StashMessage { get; set; }
    }
}
