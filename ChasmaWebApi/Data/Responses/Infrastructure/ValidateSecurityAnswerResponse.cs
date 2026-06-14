namespace ChasmaWebApi.Data.Responses.Infrastructure
{
    /// <summary>
    /// Class representing a response to a request to validate a user's answer to a security question for password reset purposes.
    /// </summary>
    public class ValidateSecurityAnswerResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the provided answer to the security question is valid for the specified user account.
        /// </summary>
        public bool IsAnswerValid { get; set; }
    }
}
