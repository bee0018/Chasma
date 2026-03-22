using ChasmaWebApi.Util;

namespace ChasmaWebApi.Data.Requests.Status
{
    /// <summary>
    /// Class representing components for staging/unstaging multiple files.
    /// </summary>
    public class ApplyBulkStagingActionRequest : ChasmaXmlBase
    {
        /// <summary>
        /// Gets or sets the repository key.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Gets or sets the file names to stage.
        /// </summary>
        public List<string> FileNames { get; set; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the file is being staged.
        /// </summary>
        public bool IsStaging { get; set; }
    }
}
