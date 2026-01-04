using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Status;

/// <summary>
/// Class representing the details of the GitHub Workflow runs.
/// </summary>
public class GitHubWorkflowRunResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string RepositoryName { get; set; }
    
    /// <summary>
    /// Gets or sets the number of builds reported.
    /// </summary>
    public int BuildCount {get; set;}

    /// <summary>
    /// Gets or sets the work flow run statuses.
    /// </summary>
    public List<WorkflowRunResult> WorkflowRunResults { get; set; } = new();
}