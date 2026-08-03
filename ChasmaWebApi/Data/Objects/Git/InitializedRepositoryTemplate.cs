using ChasmaWebApi.Data.Objects.Application;

namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing an initialized repository template.
    /// </summary>
    public class InitializedRepositoryTemplate
    {
        /// <summary>
        /// Gets or sets the user associated with the initialized repository.
        /// </summary>
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Gets or sets the local git repository associated with the initialized repository.
        /// </summary>
        public LocalGitRepository Repository { get; set; }

        /// <summary>
        /// Gets or sets the working directory of the initialized repository.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Gets or sets the commit message for the initial commit of the initialized repository.
        /// </summary>
        public string CommitMessage { get; set; }

        /// <summary>
        /// Gets or sets the name of the head branch for the initialized repository.
        /// </summary>
        public string HeadBranchName { get; set; }
    }
}
