using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Responses.Configuration
{
    /// <summary>
    /// Class representing the response for applying a work context snapshot.
    /// </summary>
    public class ApplyWorkContextSnapshotResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of repository snap shot addition results.
        /// </summary>
        public List<RepositorySnapshotAdditionResult> AdditionResults { get; set; } = [];
    }
}
