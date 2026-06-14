namespace ChasmaWebApi.Data.Responses.Infrastructure
{
    /// <summary>
    /// Class representing a response to a request to reset a user's password in the application.
    /// </summary>
    public class ResetPasswordResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the password reset operation was successful, meaning the user's password was updated in the database with the new hashed password and salt.
        /// </summary>
        public bool SuccessfullyReset { get; set; }
    }
}
