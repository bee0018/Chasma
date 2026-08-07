namespace ChasmaWebApi.Data.Objects.Application
{
    /// <summary>
    /// Class representing the remote host platforms.
    /// </summary>
    public enum RemoteHostPlatform
    {
        /// <summary>
        /// Unknown remote host platform.
        /// </summary>
        Unknown,

        /// <summary>
        /// GitHub remote host platform.
        /// </summary>
        GitHub,

        /// <summary>
        /// GitLab remote host platform.
        /// </summary>
        GitLab,

        /// <summary>
        /// Bitbucket remote host platform.
        /// </summary>
        Bitbucket,

        /// <summary>
        /// Offline repository disconnected from a remote host.
        /// </summary>
        Local,

        /// <summary>
        /// Azure DevOps remote host platform.
        /// </summary>
        AzureDevOps,

        /// <summary>
        /// AWS CodeCommit remote host platform.
        /// </summary>
        AWSCodeCommit,

        /// <summary>
        /// LaunchPad remote host platform.
        /// </summary>
        LaunchPad,
        
        /// <summary>
        /// Custom-hosted remote host platform.
        /// </summary>
        Custom,
    }
}
