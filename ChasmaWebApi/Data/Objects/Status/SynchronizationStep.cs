namespace ChasmaWebApi.Data.Objects.Status
{
    /// <summary>
    /// Enum representing the different steps involved in synchronizing a repository state in a git repository.
    /// </summary>
    public enum SynchronizationStep
    {
        /// <summary>
        /// Indicates that pre-flight checks are being performed before synchronizing the repository state.
        /// </summary>
        PreFlightChecks = 0,

        /// <summary>
        /// Indicates the step of pulling changes from the remote repository to the local repository.
        /// </summary>
        PullChanges = 1,

        /// <summary>
        /// Indicates the step of pushing changes from the local repository to the remote repository.
        /// </summary>
        PushChanges = 2,
    }
}
