namespace ChasmaWebApi.Data.Objects.Remote
{
    /// <summary>
    /// Class representing a newly created merge request after being created on GitLab.
    /// </summary>
    public class MergeRequestResult
    {
        /// <summary>
        /// Gets or sets the merge request number that is assigned to the merge request.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the URL of the merge request to be viewed in browser.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the timestamp the merge request is created.
        /// </summary>
        public string TimeStamp { get; set; }
    }
}
