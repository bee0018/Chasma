using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the components needed to delete a branch.
    /// </summary>
    public class DeleteBranchRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the friendly branch name.
        /// </summary>
        public string BranchName { get; set; }
    }
}
