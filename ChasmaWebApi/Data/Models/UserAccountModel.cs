using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChasmaWebApi.Data.Models
{
    /// <summary>
    /// Class representing the user_accounts table in the database.
    /// </summary>
    [Table("user_accounts")]
    public class UserAccountModel
    {
        /// <summary>
        /// Gets or sets the identifier of the account user.
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        [Column("name")]
        public required string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the email of the account.
        /// </summary>
        [Column("email")]
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the username of the account.
        /// </summary>
        [Column("user_name")]
        public required string UserName { get; set; }

        /// <summary>
        /// Gets or sets the hashed password of the account.
        /// </summary>
        [Column("password")]
        public required string Password { get; set; }

        /// <summary>
        /// Gets or sets the salt used for hashing the password.
        /// </summary>
        [Column("salt")]
        public required byte[] Salt { get; set; }
    }
}
