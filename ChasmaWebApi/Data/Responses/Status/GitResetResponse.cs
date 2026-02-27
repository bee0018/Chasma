namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class describing the response body for a Git reset operation, which resets the current HEAD to a specified state.
    /// </summary>
    public class GitResetResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the short commit message which is usually the first line of the commit.
        /// </summary>
        public string CommitMessage { get; set; }
    }
}
