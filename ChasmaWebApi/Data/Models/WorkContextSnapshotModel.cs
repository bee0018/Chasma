using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the work_context_snapshots table in the database.
    /// </summary>
    [Table("work_context_snapshots")]
    public class WorkContextSnapshotModel
    {
        /// <summary>
        /// Gets or sets the identifier of the work context snapshot.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("snapshot_id")]
        public int SnapshotId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier associated with the work context snapshot.
        /// </summary>
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the snapshot's display name.
        /// </summary>
        [Column("display_name")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the overall working note of the work context snapshot.
        /// </summary>
        [Column("snapshot_note")]
        public string? SnapshotNote { get; set; }
    }
}
