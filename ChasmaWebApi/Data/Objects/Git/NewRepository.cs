namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the properties of a new repository added to the system.
    /// </summary>
    public class NewRepository
    {
        /// <summary>
        /// Gets or sets the working directory of the repository.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the local git repository that has been added to the system.
        /// </summary>
        public LocalGitRepository Repository { get; set; }
    }
}
