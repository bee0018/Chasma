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
        /// Gets or sets the name of the branch to checkout.
        /// </summary>
        public string BranchName { get; set; }
    }
}
