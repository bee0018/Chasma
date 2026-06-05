using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the response for adding a work context snapshot for a repository.
    /// </summary>
    public class AddWorkContextSnapshotResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly added work context snapshot.
        /// </summary>
        public WorkContextSnapshot WorkContextSnapshot { get; set; }

        /// <summary>
        /// Gets or sets the list of repository snap shot addition results.
        /// </summary>
        public List<RepositorySnapshotAdditionResult> AdditionResults { get; set; } = [];
    }
}
