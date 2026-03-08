namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a commit entry in a Git repository.
    /// </summary>
    public class CommitEntry
    {
        /// <summary>
        /// Gets or sets the commit hash, which is a unique identifier for a specific commit in a Git repository.
        /// </summary>
        public string CommitHash { get; set; }

        /// <summary>
        /// Gets or sets the commit message.
        /// </summary>
        public string Message { get; set; }
    }
}
