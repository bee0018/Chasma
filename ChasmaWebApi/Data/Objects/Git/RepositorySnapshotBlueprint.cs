namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing a repository context snapshot entry.
    /// </summary>
    public class RepositorySnapshotBlueprint
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the snapshot note associated with the repository working context.
        /// </summary>
        public string? IntentNote { get; set; }
    }
}
