namespace ChasmaWebApi.Data.Objects.Application
{
    /// <summary>
    /// Class representing the user of the system.
    /// </summary>
    public class ApplicationUser
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's username.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the user's email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the permissions of the user.
        /// </summary>
        public ApplicationUserPermissions Permissions { get; set; }
    }
}
