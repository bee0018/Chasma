using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Interfaces;

/// <summary>
/// Interface containing the members on the GitHub Workflow Manager.
/// </summary>
public interface IWorkFlowManager
{
    /// <summary>
    /// Tries to get the workflow run results for the repo with specified details.
    /// </summary>
    /// <param name="repoName">The repository name.</param>
    /// <param name="repoOwner">The repository owner.</param>
    /// <param name="token">The GitHub access token.</param>
    /// <param name="buildCount">The threshold representing the max number of runs to retrieve.</param>
    /// <param name="workflowRunResults">The list of workflow run results.</param>
    /// <param name="errorMessage">The error message if there was a failure to retrieve runs.</param>
    /// <returns>True if the workflow runs were retrieved; false otherwise.</returns>
    bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount,
        out List<WorkflowRunResult> workflowRunResults, out string errorMessage);

    /// <summary>
    /// Finds the local git repositories on the local machine.
    /// </summary>
    List<string> FindLocalGitRepositories();
}