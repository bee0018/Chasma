namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the blueprint for cloning a git repository.
    /// </summary>
    public class GitCloneBlueprint
    {
        /// <summary>
        /// Gets or sets the user identifier that the repositories will be associated to.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the URL of the git repository to clone.
        /// </summary>
        public string SourceUrl { get; set; }

        /// <summary>
        /// Gets or sets the local path where the repository should be cloned to.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to clone submodules recursively.
        /// </summary>
        public bool RecurseSubmodules { get; set; }
    }
}
