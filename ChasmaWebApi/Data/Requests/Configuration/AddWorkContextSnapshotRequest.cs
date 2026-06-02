using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Requests.Configuration
{
    /// <summary>
    /// Class representing the request to add a work context snapshot for a repository.
    /// </summary>
    public class AddWorkContextSnapshotRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the snapshot display name.
        /// </summary>
        public string SnapshotDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the repository snapshot entries.
        /// </summary>
        public List<RepositorySnapshotBlueprint> Blueprints { get; set; } = [];

        /// <summary>
        /// Gets or sets the overall working note of the work context snapshot.
        /// </summary>
        public string? SnapshotNote { get; set; }
    }
}
