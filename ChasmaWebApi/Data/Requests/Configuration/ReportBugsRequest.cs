namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing a request to report a system bug.
    /// </summary>
    public class ReportBugsRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string IssueTitle { get; set; }

        /// <summary>
        /// Gets or sets description of the user reported bug.
        /// </summary>
        public string BugDescription { get; set; }
    }
}
