namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing a response containing the Git diff for a specific file in a repository.
    /// </summary>
    public class GitDiffResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the content of the Git diff.
        /// </summary>
        public string DiffContent { get; set; }
    }
}
