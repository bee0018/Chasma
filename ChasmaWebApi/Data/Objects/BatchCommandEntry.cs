namespace ChasmaWebApi.Data.Objects
{
    /// <summary>
    /// Class representing a batch command entry.
    /// </summary>
    public class BatchCommandEntry
    {
        /// <summary>
        /// Gets or sets the repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the list of commands to execute.
        /// </summary>
        public List<string> Commands { get; set; } = new();
    }
}
