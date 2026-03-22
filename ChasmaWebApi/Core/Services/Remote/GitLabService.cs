using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Util;
using NGitLab;
using NGitLab.Models;

namespace ChasmaWebApi.Core.Services.Remote
{
    /// <summary>
    /// Class representing the service conducting remote operations to the GitLab API.
    /// </summary>
    public class GitLabService : IGitLabService
    {
        /// <summary>
        /// The internal logging instance.
        /// </summary>
        private readonly ILogger<GitLabService> logger;

        /// <summary>
        /// The internal API configuration.
        /// </summary>
        private readonly ChasmaWebApiConfigurations configurations;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// Gets or sets the GitLab API client.
        /// </summary>
        private static GitLabClient Client { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitLabService"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="config">The web API configurations.</param>
        /// <param name="apiCacheManager">The API cache manager.</param>
        public GitLabService(ILogger<GitLabService> log, ChasmaWebApiConfigurations config, ICacheManager apiCacheManager)
        {
            logger = log;
            configurations = config;
            cacheManager = apiCacheManager;
        }

        // <inheritdoc />
        public bool TryGetPipelineJobResults(LocalGitRepository repository, out List<WorkflowRunResult> buildResults, out string errorMessage)
        {
            errorMessage = string.Empty;
            buildResults = new();
            try
            {
                Task<List<Job>?> pipelineTask = GetPipelineJobs(repository.Owner, repository.Name);
                List<Job>? pipelineJobs = pipelineTask.Result;
                if (pipelineJobs == null)
                {
                    errorMessage = $"Failed to fetch pipeline jobs for {repository.Name}. Check server logs for more information.";
                    return false;
                }

                foreach (Job job in pipelineJobs)
                {
                    WorkflowRunResult buildResult = new()
                    {
                        BranchName = job.Ref,
                        RunNumber = job.Id,
                        BuildTrigger = job.Stage,
                        CommitMessage = job.Commit.Message,
                        BuildStatus = job.Pipeline.Status.ToString().ToLower(),
                        BuildConclusion = job.Status.ToString().ToLower(),
                        CreatedDate = job.CreatedAt.ToLocalTime().ToString("g"),
                        UpdatedDate = job.CreatedAt.ToLocalTime().ToString("g"),
                        WorkflowUrl = job.WebUrl,
                        AuthorName = job.User.Name,
                    };
                    buildResults.Add(buildResult);
                }

                return true;
            }
            catch (Exception e)
            {
                errorMessage = "Error occurred when attempting to fetch pipeline jobs. Review server logs.";
                logger.LogError("Could not get pipeline job results and is now sending error response. Error {error}", e);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryCreateIssue(PreparedGitLabIssue issueCreation, out GitLabIssueResult issue, out string errorMessage)
        {
            errorMessage = string.Empty;
            issue = null;
            try
            {
                Task<Issue?> responseTask = SendCreateIssueRequest(issueCreation);
                Issue gitLabIssue = responseTask.Result;
                if (gitLabIssue == null)
                {
                    errorMessage = "Failed to create issue. Review server logs for more information.";
                    logger.LogError("Could not GitLab issue. Sending error response.");
                    return false;
                }

                issue = new()
                {
                    IssueId = gitLabIssue.IssueId,
                    Url = gitLabIssue.WebUrl,
                };
                return true;
            }
            catch (Exception e)
            {
                errorMessage = "Error occurred when attempting to create GitLab issue. Review server logs.";
                logger.LogError("Could not create GitLab issue and is now sending error response. Error {error}", e);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryGetUsersInProject(LocalGitRepository repository, out List<GitLabProjectMember> projectMembers, out long projectId, out string errorMessage)
        {
            errorMessage = string.Empty;
            projectMembers = new();
            projectId = -1;
            try
            {
                Task<(List<Membership> Members, long ProjectId)?> responseTask = SendGetUsersInProjectRequest(repository.Owner, repository.Name);
                (List<Membership> Members, long ProjectId)? membershipResult = responseTask.Result;
                if (membershipResult == null)
                {
                    errorMessage = $"Failed to get members in {repository.Name}. Review server logs for more information.";
                    logger.LogError("Could not get members in {repo}. Sending error response.", repository.Name);
                    return false;
                }

                foreach (Membership member in membershipResult.Value.Members)
                {
                    GitLabProjectMember projectMember = new()
                    {
                        AssigneeId = member.Id,
                        UserName = member.UserName,
                        FullName = member.Name,
                    };
                    projectMembers.Add(projectMember);
                }

                projectId = membershipResult.Value.ProjectId;
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Error when trying to get members. Review server logs for more information.";
                logger.LogError("Error when trying to get members in {repo} project. Error: {error}", repository.Name, e);
                return false;
            }
        }

        // <inheritdoc />
        public bool TryCreateMergeRequest(PreparedGitLabMergeRequest preparedMergeRequest, out MergeRequestResult mergeResult, out string errorMessage)
        {
            mergeResult = null;
            errorMessage = string.Empty;
            try
            {
                Task<MergeRequest?> responseTask = SendCreateMergeRequest(preparedMergeRequest);
                MergeRequest? gitLabMergeRequest = responseTask.Result;
                if (gitLabMergeRequest == null)
                {
                    errorMessage = "Failed to create merge request. Review server logs for more information.";
                    logger.LogError("Could not GitLab merge request. Sending error response.");
                    return false;
                }

                mergeResult = new()
                {
                    Id = gitLabMergeRequest.Id,
                    Url = gitLabMergeRequest.WebUrl,
                    TimeStamp = gitLabMergeRequest.CreatedAt.ToLocalTime().ToString("g"),
                };
                RemotePullRequest mr = new()
                {
                    Number = gitLabMergeRequest.Iid,
                    RepositoryName = preparedMergeRequest.RepoName,
                    RepositoryOwner = preparedMergeRequest.RepoOwner,
                    BranchName = gitLabMergeRequest.SourceBranch,
                    ActiveState = gitLabMergeRequest.State,
                    MergeableState = gitLabMergeRequest.DetailedMergeStatus.StringValue,
                    CreatedAt = gitLabMergeRequest.CreatedAt.ToLocalTime().ToString("g"),
                    MergedAt = gitLabMergeRequest.MergedAt.HasValue ? gitLabMergeRequest.MergedAt.Value.ToLocalTime().ToString("g") : null,
                    Merged = gitLabMergeRequest.MergedAt.HasValue,
                    HtmlUrl = gitLabMergeRequest.WebUrl
                };
                cacheManager.GitLabMergeRequests.TryAdd(mr.Number, mr);
                logger.LogInformation("Created merge request {mergeId} in {repoName}.", mr.Number, preparedMergeRequest.RepoName);
                return true;
            }
            catch (Exception e)
            {
                errorMessage = $"Error when trying to create merge request. Review server logs for more information.";
                logger.LogError("Error when trying to create GitLab merge request in {repo} project. Error: {error}", preparedMergeRequest.RepoName, e);
                return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the pipeline jobs from the GitLab API.
        /// </summary>
        /// <param name="owner">The repository owner.</param>
        /// <param name="repoName">The repository name.</param>
        /// <returns>The pipeline builds from GitLab.</returns>
        private async Task<List<Job>?> GetPipelineJobs(string owner, string repoName)
        {
            try
            {
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                Project project = await Client.Projects.GetAsync($"{owner}/{repoName}");
                if (project == null)
                {
                    logger.LogError("Could not find project on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                IPipelineClient pipelineClient = Client.GetPipelines(project.Id);
                PipelineBasic? latestPipeline = pipelineClient.All
                                   .OrderByDescending(p => p.Id)
                                   .FirstOrDefault();
                if (latestPipeline == null)
                {
                    logger.LogError("Could not get latest pipeline for repo: {repo}", repoName);
                    return null;
                }

                IJobClient jobClient = Client.GetJobs(project.Id);
                JobQuery query = new()
                {
                    Scope = JobScopeMask.All,
                    PerPage = configurations.WorkflowRunReportThreshold,
                };

                return jobClient.GetJobs(query)
                    .Where(i => i.Pipeline.Id == latestPipeline.Id)
                    .ToList();
            }
            catch (Exception e)
            {
                logger.LogError("Failed to get pipeline jobs from the GitLab API: {error}", e);
                return null;
            }
        }

        /// <summary>
        /// Gets the users in the specified project.
        /// </summary>
        /// <param name="owner">The owner of the repository.</param>
        /// <param name="repoName">The repository name.</param>
        /// <returns>The list of users in the project member listing.</returns>
        private async Task<(List<Membership> Members, long ProjectId)?> SendGetUsersInProjectRequest(string owner, string repoName)
        {
            try
            {
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                Project project = await Client.Projects.GetAsync($"{owner}/{repoName}");
                if (project == null)
                {
                    logger.LogError("Could not find project on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                List<GitLabProjectMember> projectMembers = new();
                List<Membership> members = Client.Members.OfProjectAsync(project.Id, true).ToList();
                return (members, project.Id);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to get users from the GitLab API: {error}", e);
                return null;
            }
        }

        /// <summary>
        /// Sends a request to the GitLab API to create an issue.
        /// </summary>
        /// <param name="issue">The issue creation details.</param>
        /// <returns>The newly created issue from GitLab.</returns>
        private async Task<Issue?> SendCreateIssueRequest(PreparedGitLabIssue issue)
        {
            try
            {
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                Project project = await Client.Projects.GetAsync($"{issue.RepoOwner}/{issue.RepoName}");
                IssueCreate issueRequest = new()
                {
                    ProjectId = project.Id,
                    AssigneeId = issue.MainAssignee?.AssigneeId,
                    AssigneeIds = issue.Contacts.Select(i => i.AssigneeId).ToArray(),
                    Title = issue.Title,
                    Description = issue.Description,
                    Confidential = issue.Confidential,

                };
                Issue newIssue = await Client.Issues.CreateAsync(issueRequest);
                return newIssue;
            }
            catch (Exception e)
            {
                logger.LogError("Failed to create issue via the GitLab API: {error}", e);
                return null;
            }
        }

        /// <summary>
        /// Sends a request to the GitLab API to create a merge request with the specified details.
        /// </summary>
        /// <param name="preparedMergeRequest">The merge request outline template.</param>
        /// <returns>The newly created merge request details from the GitLab API.</returns>
        private async Task<MergeRequest?> SendCreateMergeRequest(PreparedGitLabMergeRequest preparedMergeRequest)
        {
            try
            {
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken, configurations.SelfHostedGitLabUrl);
                Project project = await Client.Projects.GetAsync($"{preparedMergeRequest.RepoOwner}/{preparedMergeRequest.RepoName}");
                IMergeRequestClient mergeRequestClient = Client.GetMergeRequest(project.Id);
                MergeRequestCreate mergeRequestToCreate = new()
                {
                    SourceBranch = preparedMergeRequest.SourceBranch,
                    TargetBranch = preparedMergeRequest.TargetBranch,
                    Title = preparedMergeRequest.Title,
                    Description = preparedMergeRequest.Description,
                    AssigneeId = preparedMergeRequest.Assignee?.AssigneeId,
                    AssigneeIds = preparedMergeRequest.AdditonalAssignees.Select(i => i.AssigneeId).ToArray(),
                    ReviewerIds = preparedMergeRequest.Reviewers.Select(i => i.AssigneeId).ToArray(),
                    RemoveSourceBranch = preparedMergeRequest.RemoveSourceBranch,
                    Squash = preparedMergeRequest.Squash,
                    AllowCollaboration = preparedMergeRequest.AllowCollaboration,
                };
                MergeRequest mergeRequest = mergeRequestClient.Create(mergeRequestToCreate);
                return mergeRequest;
            }
            catch (Exception e)
            {
                logger.LogError("Failed to create merge request via the GitLab API: {error}", e);
                return null;
            }
        }

        #endregion
    }
}
