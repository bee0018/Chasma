namespace ChasmaWebApi.Data.Objects;

/// <summary>
/// Class representing the GitHub workflow status results.
/// </summary>
public class WorkflowRunResult
{
    /// <summary>
    /// Gets or sets the branch name.
    /// </summary>
    public string BranchName { get; set; }
    
    /// <summary>
    /// Gets or sets the run number.
    /// </summary>
    public long RunNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the build trigger.
    /// </summary>
    public string BuildTrigger { get; set; }
    
    /// <summary>
    /// Gets or sets the commit display title.
    /// </summary>
    public string CommitMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the build status result.
    /// </summary>
    public string BuildStatus { get; set; }
    
    /// <summary>
    /// Gets or sets the workflow conclusion result.
    /// </summary>
    public string BuildConclusion { get; set; }
    
    /// <summary>
    /// Gets or sets the created date of the workflow run.
    /// </summary>
    public string CreatedDate { get; set; }
    
    /// <summary>
    /// Gets or sets the updated date of the workflow run.
    /// </summary>
    public string UpdatedDate { get; set; }
    
    /// <summary>
    /// Gets or sets the workflow run URL for reference.
    /// </summary>
    public string WorkflowUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the committer's name.
    /// </summary>
    public string AuthorName { get; set; }
}