namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the components of a request to add a user.
    /// </summary>
    public class AddUserRequest
    {
        /// <summary>
        /// Gets or sets the full name of the user.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        public string UserName { get; set; }
        
        /// <summary>
        /// Gets or sets the email of the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the first security question for account recovery.
        /// </summary>
        public string FirstSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the first security question.
        /// </summary>
        public string FirstSecurityAnswer { get; set; }

        /// <summary>
        /// Gets or sets the second security question for account recovery.
        /// </summary>
        public string SecondSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the second security question.
        /// </summary>
        public string SecondSecurityAnswer { get; set; }

        /// <summary>
        /// Gets or sets the third security question for account recovery.
        /// </summary>
        public string ThirdSecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the third security question.
        /// </summary>
        public string ThirdSecurityAnswer { get; set; }
    }
}
