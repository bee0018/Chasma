using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class describing the request body for a Git reset operation, which resets the current HEAD to a specified state.
    /// </summary>
    public class GitResetRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the unique identifier of the repository.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the reset mode, which determines how the reset operation will affect the working directory and staging area.
        /// </summary>
        public ResetMode ResetMode { get; set; }

        /// <summary>
        /// Gets or sets the revision specification, which specifies the target commit or reference to reset to (e.g., a branch name, tag name, or commit hash).
        /// </summary>
        public string RevParseSpec { get; set; }
    }
}
