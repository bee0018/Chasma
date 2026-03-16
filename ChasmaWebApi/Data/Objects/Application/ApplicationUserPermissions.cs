namespace ChasmaWebApi.Data.Objects.Application
{
    /// <summary>
    /// Class representing the permissions of the logged in user in the system.
    /// </summary>
    public class ApplicationUserPermissions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the user is using the GitHub remote hosting platform API.
        /// </summary>
        public bool IsUsingGitHubApi { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is using the GitLab remote hosting platform API.
        /// </summary>
        public bool IsUsingGitLabApi { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is using the Bitbucket remote hosting platform API.
        /// </summary>
        public bool IsUsingBitbucketApi { get; set; }
    }
}
