using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Class representing the response to the newly created GitLab merge request.
    /// </summary>
    public class CreateMergeRequestResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the newly created merge request.
        /// </summary>
        public MergeRequestResult MergeRequest { get; set; }
    }
}
