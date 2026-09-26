namespace ChasmaWebApi.Data.Requests.Remote;

/// <summary>
/// Class representing the request to receive workflow runs with the specified details.
/// </summary>
public class GetWorkflowResultsRequest
{
    /// <summary>
    /// Gets or sets the repository identifier for which to retrieve workflow runs.
    /// </summary>
    public string RepositoryId { get; set; }

    /// <summary>
    /// Gets or sets the branch name for which to retrieve workflow runs.
    /// </summary>
    public string BranchName { get; set; }
}