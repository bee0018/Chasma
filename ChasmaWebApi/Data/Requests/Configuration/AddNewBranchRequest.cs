using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to add a new branch to a repository.
    /// </summary>
    public class AddNewBranchRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the branch name to be added.
        /// </summary>
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the new branch should be checked out after creation.
        /// </summary>
        public bool IsCheckingOutNewBranch { get; set; }
    }
}
