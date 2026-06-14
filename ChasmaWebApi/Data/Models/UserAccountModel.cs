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

        /// <summary>
        /// Gets or sets the user's refresh token.
        /// Note: This is the token used for extending user access.
        /// </summary>
        [Column("refreshToken")]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the refresh token.
        /// </summary>
        [Column("refreshTokenExpiration")]
        public DateTime RefreshTokenExpiration { get; set; }

        /// <summary>
        /// Gets or sets the first security question for account recovery.
        /// </summary>
        [Column("first_security_question")]
        public string FirstSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the first security question.
        /// </summary>
        [Column("first_security_answer")]
        public string FirstSecurityAnswer { get; set; }

        /// <summary>
        /// Gets or sets the salt used for hashing the answer to the first security question.
        /// </summary>
        [Column("first_security_answer_salt")]
        public byte[] FirstSecurityAnswerSalt { get; set; }

        /// <summary>
        /// Gets or sets the second security question for account recovery.
        /// </summary>
        [Column("second_security_question")]
        public string SecondSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the second security question.
        /// </summary>
        [Column("second_security_answer")]
        public string SecondSecurityAnswer { get; set; }

        /// <summary>
        /// Gets or sets the salt used for hashing the answer to the second security question.
        /// </summary>
        [Column("second_security_answer_salt")]
        public byte[] SecondSecurityAnswerSalt { get; set; }

        /// <summary>
        /// Gets or sets the third security question for account recovery.
        /// </summary>
        [Column("third_security_question")]
        public string ThirdSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the third security question.
        /// </summary>
        [Column("third_security_answer")]
        public string ThirdSecurityAnswer { get; set; }

        /// <summary>
        /// Gets or sets the salt used for hashing the answer to the third security question.
        /// </summary>
        [Column("third_security_answer_salt")]
        public byte[] ThirdSecurityAnswerSalt { get; set; }
    }
}
