using ChasmaWebApi.Data.Objects.Git;
using LibGit2Sharp;

namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing an entry in the merge conflict result package, containing information about the output path, worktree path, conflicting files, repository, source and destination branches, and the base commit.
    /// </summary>
    public class MergeConflictResultPackageEntry
    {
        /// <summary>
        /// Gets or sets the directory where the merge conflict results package is created.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the timestamp indicating when the merge conflict results package was created, which can be useful for tracking and auditing purposes.
        /// </summary>
        public string TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the path to the working tree containing the conflicted files.
        /// </summary>
        public string WorktreePath { get; set; }

        /// <summary>
        /// Gets or sets the collection of file paths representing files in conflict.
        /// </summary>
        public List<string> ConflictingFiles { get; set; } = [];

        /// <summary>
        /// Gets or sets the local Git repository associated with the merge operation.
        /// </summary>
        public LocalGitRepository Repository { get; set; }

        /// <summary>
        /// Gets or sets the source branch involved in the merge operation.
        /// </summary>
        public Branch SourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the destination branch involved in the merge operation.
        /// </summary>
        public Branch DestinationBranch { get; set; }

        /// <summary>
        /// Gets or sets the base commit from which the source and destination branches diverged, which is relevant for understanding the context of the merge conflict.
        /// </summary>
        public Commit BaseCommit { get; set; }
    }
}
