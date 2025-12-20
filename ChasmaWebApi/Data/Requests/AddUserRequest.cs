namespace ChasmaWebApi.Data.Requests
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
        /// Gets or sets the user name of the user.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        public string Password { get; set; }
    }
}
