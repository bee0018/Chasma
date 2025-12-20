using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the working_directories table in the database.
    /// </summary>
    [Table("working_directories")]
    public class WorkingDirectoryModel
    {
        /// <summary>
        /// Gets or sets the identifier of the working directory.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier that this working directory belongs to.
        /// </summary>
        [Column("repository_id")]
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the working directory path.
        /// </summary>
        [Column("working_directory")]
        public string WorkingDirectory { get; set; }
    }
}
