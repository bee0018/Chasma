namespace ChasmaWebApi.Data.Requests.Status;

/// <summary>
/// Class representing the request to receive workflow runs with the specified details.
/// </summary>
public class GetWorkflowResultsRequest
{
    /// <summary>
    /// Gets or sets the repository name.
    /// </summary>
    public string RepositoryName { get; set; }
    
    /// <summary>
    /// Gets or sets the repository owner name.
    /// </summary>
    public string RepositoryOwner { get; set; }
}