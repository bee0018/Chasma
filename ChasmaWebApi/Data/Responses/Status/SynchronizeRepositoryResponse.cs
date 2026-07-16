namespace ChasmaWebApi.Data.Responses.Status
{
    /// <summary>
    /// Class representing a response to a synchronizing a repository state in a git repository.
    /// </summary>
    public class SynchronizeRepositoryResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the description of the synchronization step that was performed.
        /// </summary>
        public string SyncStepDescription { get; set; }
    }
}
