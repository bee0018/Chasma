using ChasmaWebApi.Util;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a repository status element.
    /// </summary>
    public class RepositoryStatusElement : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository identifier this file is associated to.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the file path relative to the repository directory.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the file state.
        /// Examples: DeletedFromWorkdir, ModifiedInWorkdir, NewInWorkdir, Ignored, etc.
        /// </summary>
        public FileStatus State { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this file is staged or not.
        /// </summary>
        public bool IsStaged { get; set; }
    }
}
