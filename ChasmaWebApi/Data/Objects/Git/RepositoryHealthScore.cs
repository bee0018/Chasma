namespace ChasmaWebApi.Data.Objects.Git
{
    /// <summary>
    /// Class representing the health score of a Git repository, which may include various metrics and indicators of the repository's overall health and maintainability.
    /// </summary>
    public class RepositoryHealthScore
    {
        /// <summary>
        /// Gets or sets the health score of the repository.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Gets or sets the category of the health score, which may indicate the level of health (e.g., "Good", "Moderate", "Poor") based on the score value.
        /// </summary>
        public string ScoreCategory { get; set; }

        /// <summary>
        /// Gets or sets the description of the health score, providing context or details about what the score represents.
        /// </summary>
        public List<string> Description { get; set; } = [];
    }
}
