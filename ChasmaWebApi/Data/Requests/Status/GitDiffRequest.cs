using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing a request to get the Git diff for a specific file in a repository.
    /// </summary>
    public class GitDiffRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the file path to get the diff for.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to get the staged changes or unstaged changes.
        /// </summary>
        public bool IsStaged { get; set; }
    }
}
