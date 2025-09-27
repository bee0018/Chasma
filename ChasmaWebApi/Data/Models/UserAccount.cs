using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the user_accounts table in the database.
    /// </summary>
    [Table("user_accounts")]
    public class UserAccount
    {
        /// <summary>
        /// Gets or sets the identifier of the account user.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets  the name of the account.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the user name of the account.
        /// </summary>
        [Column("user_name")]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password of the account.
        /// </summary>
        [Column("password")]
        public string Password { get; set; }
    }
}
