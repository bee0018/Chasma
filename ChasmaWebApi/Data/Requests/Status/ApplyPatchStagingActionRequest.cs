
using ChasmaWebApi.Data.Objects.Git;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing the request to partially apply a stage operation on a selected patch.
    /// </summary>
    public class ApplyPatchStagingActionRequest
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the file to apply the stage operation for.
        /// </summary>
        public RepositoryStatusElement File {  get; set; }

        /// <summary>
        /// Gets or sets the line to begin staging operation for.
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the line to end the staging operation for.
        /// </summary>
        public int EndLine { get; set; }
    }
}
