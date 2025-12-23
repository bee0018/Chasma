namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing the components of a response to adding a user.
    /// </summary>
    public class AddUserResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the username of the newly added user.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the newly added user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the email of the newly added user.
        /// </summary>
        public string Email { get; set; }
    }
}
