namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the result of an attempt to add a workspace snapshot to the system.
    /// </summary>
    public class RepositorySnapshotAdditionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the addition of a workspace snapshot was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the reason why the snapshot addition failed.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the name of the workspace snapshot that was added to the system.
        /// </summary>
        public string SnapshotName { get; set; }

        /// <summary>
        /// Gets or sets the name of the repository to which the workspace snapshot belongs.
        /// </summary>
        public string RepositoryName { get; set; }
    }
}
