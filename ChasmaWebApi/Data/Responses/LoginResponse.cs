namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the components of a response to a login request.
    /// </summary>
    public class LoginResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the username of the logged in user.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }
    }
}
