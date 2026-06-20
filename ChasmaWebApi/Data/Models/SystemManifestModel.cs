using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the system_manifests model table.
    /// </summary>
    [Table("system_manifests")]
    public class SystemManifestModel
    {
        /// <summary>
        /// Gets or sets the identifier of the metadata manifest row.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the version of the manifest downloaded.
        /// </summary>
        [Column("version")]
        public string Version { get; set; }
    }
}
