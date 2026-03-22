using ChasmaWebApi.Data.Objects.Remote;

namespace ChasmaWebApi.Data.Responses.Remote
{
    /// <summary>
    /// Class representing the response to getting pipeline jobs from GitLab.
    /// </summary>
    public class GetPipelineJobsResponse : ResponseBase
    {
        /// <summary>
        /// Gets or sets the list of results of pipeline jobs from GitLab.
        /// </summary>
        public List<WorkflowRunResult> Results { get; set; } = new();
    }
}
