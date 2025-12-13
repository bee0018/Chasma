using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using Octokit;

namespace ChasmaWebApi.Data.Managers;

/// <summary>
/// Class representing the manager for processing workflow run data.
/// </summary>
/// <param name="logger">The internal server logger.</param>
public class WorkflowManager(ILogger<WorkflowManager> logger)
    : ClientManagerBase<WorkflowManager>(logger), IWorkFlowManager
{
    // <inheritdoc/>
    public bool TryGetWorkflowRunResults(string repoName, string repoOwner, string token, int buildCount, out List<WorkflowRunResult> workflowRunResults, out string errorMessage)
    {
        errorMessage = string.Empty;
        workflowRunResults = new();
        ProductHeaderValue productHeader = new ProductHeaderValue(repoName);
        Client = new(productHeader) { Credentials = new Credentials(token) };
        Task<WorkflowRunsResponse?> workflowRunsResponseTask = GetWorkFlowRuns(Client, repoOwner, repoName);
        WorkflowRunsResponse workFlowRunsResponse = workflowRunsResponseTask.Result;
        if (workFlowRunsResponse == null)
        {
            errorMessage = $"Failed to fetch workflow runs for {repoName}. Check server logs for more information.";
            return false;
        }
        
        List<WorkflowRun> runs = workFlowRunsResponse.WorkflowRuns.Take(buildCount).ToList();
        foreach (WorkflowRun run in runs)
        {
            WorkflowRunResult buildResult = new()
            {
                BranchName = run.HeadBranch,
                RunNumber = run.RunNumber,
                BuildTrigger = run.Event,
                CommitMessage = run.HeadCommit.Message,
                BuildStatus = run.Status.StringValue,
                BuildConclusion = run.Conclusion.HasValue ? run.Conclusion.Value.ToString() : "Unknown",
                CreatedDate = run.CreatedAt.ToString("g"),
                UpdatedDate = run.UpdatedAt.ToString("g"),
                WorkflowUrl = run.HtmlUrl,
                AuthorName = run.Actor.Login,
            };
            workflowRunResults.Add(buildResult);
        }
        
        ClientLogger.LogInformation("Retrieved {count} build runs from {repo}.", runs.Count, repoName);
        return true;
    }
    
    /// <summary>
    /// Gets the workflow run for the specified repository.
    /// </summary>
    /// <param name="client">The GitHub API client.</param>
    /// <param name="repoOwner">The repository owner.</param>
    /// <param name="repoName">The repository name.</param>
    /// <returns>Task containing the workflow run response from the API client.</returns>
    private async Task<WorkflowRunsResponse?> GetWorkFlowRuns(GitHubClient client, string repoOwner, string repoName)
    {
        try
        {
            return await client.Actions.Workflows.Runs.List(repoOwner, repoName);
        }
        catch (Exception e)
        {
            ClientLogger.LogError("Error when trying to retrieve workflow runs for {repoName}: {error}", repoName, e);
            return null;
        }
    }
}