using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the repository_snapshots table in the database.
    /// </summary>
    [Table("repository_snapshots")]
    public class RepositoryWorkContextSnapshotModel
    {
        /// <summary>
        /// Gets or sets a unique identifier for this specific repository snapshot row.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("database_id")]
        public int DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the parent work context snapshot.
        /// </summary>
        [ForeignKey(nameof(WorkContextSnapshotModel))]
        [Column("snapshot_id")]
        public int SnapshotId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the repository.
        /// </summary>
        [Column("repository_id")]
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the branch name of the snapshot.
        /// </summary>
        [Column("branch_name")]
        public string BranchName { get; set; }

        /// <summary>
        /// Gets or sets the commit hash of the snapshot.
        /// </summary>
        [Column("commit_hash")]
        public string CommitHash { get; set; }

        /// <summary>
        /// Gets or sets the time at which the snapshot was created.
        /// </summary>
        [Column("created_at")]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the stash message of the snapshot.
        /// </summary>
        [Column("stash_message")]
        public string? StashMessage { get; set; }

        /// <summary>
        /// Gets or sets the note of intent of the workspace.
        /// </summary>
        [Column("intent_note")]
        public string? IntentNote { get; set; }
    }
}
