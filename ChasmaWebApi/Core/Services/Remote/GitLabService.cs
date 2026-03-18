using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using LibGit2Sharp;
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
        /// Gets or sets the GitLab API client.
        /// </summary>
        private static GitLabClient Client { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GitLabService"/> class.
        /// </summary>
        /// <param name="log">The internal API logger.</param>
        /// <param name="config">The web API configurations.</param>
        public GitLabService(ILogger<GitLabService> log, ChasmaWebApiConfigurations config)
        {
            logger = log;
            configurations = config;
        }

        // <inheritdoc />
        public bool TryGetPipelineJobResults(string workingDirectory, LocalGitRepository cachedRepo, out List<WorkflowRunResult> buildResults, out string errorMessage)
        {
            errorMessage = string.Empty;
            buildResults = new();
            try
            {
                using Repository repo = new(workingDirectory);
                Task<List<Job>?> pipelineTask = GetPipelineJobs(cachedRepo.Owner, cachedRepo.Name);
                List<Job>? pipelineJobs = pipelineTask.Result;
                if (pipelineJobs == null)
                {
                    errorMessage = $"Failed to fetch pipeline jobs for {cachedRepo.Name}. Check server logs for more information.";
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
        public bool TryCreateIssue(GitLabIssueCreation issueCreation, out GitLabIssueResult issue, out string errorMessage)
        {
            errorMessage = string.Empty;
            issue = null;
            try
            {
                Task<Issue> responseTask = SendCreateIssueRequest(issueCreation);
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

        public bool TryGetUsersInProject()
        {
            try
            {

            }
            catch (Exception e)
            {
                logger.LogError("");
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
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken);
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
        private async Task<List<GitLabProjectMember>?> SendGetUsersInProjectRequest(string owner, string repoName)
        {
            try
            {
                Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken);
                Project project = await Client.Projects.GetAsync($"{owner}/{repoName}");
                if (project == null)
                {
                    logger.LogError("Could not find project on GitLab with owner: {owner} and repo {repoName}", owner, repoName);
                    return null;
                }

                List<GitLabProjectMember> projectMembers = new();
                List<Membership> members = Client.Members.OfProjectAsync(project.Id, true).ToList();
                foreach (Membership member in members)
                {
                    GitLabProjectMember projectMember = new()
                    {
                        AssigneeId = member.Id,
                        UserName = member.UserName,
                        FullName = member.Name,
                    };
                    projectMembers.Add(projectMember);
                }

                return projectMembers;
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
        private async Task<Issue> SendCreateIssueRequest(GitLabIssueCreation issue)
        {
            Client = RemoteHelper.GetGitLabClient(configurations.GitLabApiToken);
            Project project = await Client.Projects.GetAsync($"{issue.RepoOwner}/{issue.RepoName}");
            IssueCreate issueRequest = new()
            {
                ProjectId = project.Id,
                AssigneeId = issue.MainAssignee.AssigneeId,
                AssigneeIds = issue.Contacts.Select(i => i.AssigneeId).ToArray(),
                Title = issue.Title,
                Description = issue.Description,
                Confidential = issue.Confidential,
                
            };
            Issue newIssue = await Client.Issues.CreateAsync(issueRequest);
            return newIssue;
        }

        #endregion
    }
}
