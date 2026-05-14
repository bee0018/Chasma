namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the result of an attempt to add a Git repository to the system.
    /// </summary>
    public class RepositoryAdditionResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the addition of a Git repository was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the reason why the addition of a Git repository failed.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the name of a repository that was added to the system.
        /// </summary>
        public string RepositoryName { get; set; }
    }
}
