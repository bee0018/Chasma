namespace ChasmaWebApi.Data.Objects.DryRun
{
    /// <summary>
    /// Class representing the base result of simulating a Git operation in a dry run result.
    /// </summary>
    public class SimulatedResultBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the simulated git pull operation was successful. A value of true indicates that the operation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets an error message describing any issues encountered during the simulation of the git pull operation.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the repository name.
        /// </summary>
        public string RepositoryName { get; set; }
    }
}