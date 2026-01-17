using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the repositories table in the database.
    /// </summary>
    [Table("repositories")]
    public class RepositoryModel
    {
        /// <summary>
        /// Gets or sets the identifier of the repository.
        /// </summary>
        [Key]
        [Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user identifier that this repository belongs to.
        /// </summary>
        [Column("userId")]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the repository owner.
        /// </summary>
        [Column("owner")]
        public string Owner { get; set; }

        /// <summary>
        /// Gets or sets the repository URL.
        /// </summary>
        [Column("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the repository is ignored.
        /// </summary>
        [Column("is_ignored")]
        public bool IsIgnored { get; set; }
    }
}
