namespace ChasmaWebApi.Data.Responses
{
    /// <summary>
    /// Class representing a response to a create pull request operation.
    /// Note: This response will be used for GitHub pull requests since it will be used with the Ocktokit library.
    /// </summary>
    public class CreatePRResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the pull request identifier.
        /// </summary>
        public int PullRequestId { get; set; }

        /// <summary>
        /// Gets or sets the pull request URL.
        /// </summary>
        public string PullRequestUrl { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the pull request was created.
        /// </summary>
        public string TimeStamp { get; set; }
    }
}
