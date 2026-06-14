namespace ChasmaWebApi.Data.Requests.Infrastructure
{
    /// <summary>
    /// Class representing the components of a request to validate a user's answer to a security question for password reset purposes.
    /// </summary>
    public class ValidateSecurityAnswerRequest
    {
        /// <summary>
        /// Gets or sets the username of the user for whom to validate the security answer.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the security question for which to validate the answer.
        /// </summary>
        public string SecurityQuestion { get; set; }

        /// <summary>
        /// Gets or sets the answer to the security question to validate.
        /// </summary>
        public string SecurityAnswer { get; set; }
    }
}
