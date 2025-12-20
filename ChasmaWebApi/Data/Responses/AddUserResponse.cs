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
    }
}
