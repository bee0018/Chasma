namespace ChasmaWebApi.Data.Requests.Infrastructure
{
    /// <summary>
    /// Class representing the components of a request to reset a user's password.
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Gets or sets the username of the user for whom to reset the password.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the new password to set for the user account.
        /// </summary>
        public string Password { get; set; }
    }
}
