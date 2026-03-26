using ChasmaWebApi.Controllers;
using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Objects.Remote;
using ChasmaWebApi.Data.Requests.Remote;
using ChasmaWebApi.Data.Responses.Remote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class testing the <see cref="RemoteController"/> class.
    /// </summary>
    [TestClass]
    public class RemoteControllerTests : ControllerTestBase<RemoteController>
    {
        /// <summary>
        /// The mocked internal logger for API testing.
        /// </summary>
        private readonly Mock<ILogger<RemoteController>> loggerMock;

        /// <summary>
        /// The mocked application control service.
        /// </summary>
        private readonly Mock<IApplicationControlService> controlServiceMock;

        /// <summary>
        /// The mocked cached manager.
        /// </summary>
        private readonly Mock<ICacheManager> cacheManagerMock;

        /// <summary>
        /// The mocked web API configurations.
        /// </summary>
        private readonly Mock<ChasmaWebApiConfigurations> webApiConfigurationsMock;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteControllerTests"/> class.
        /// </summary>
        public RemoteControllerTests()
        {
            loggerMock = new Mock<ILogger<RemoteController>>();
            controlServiceMock = new Mock<IApplicationControlService>();
            cacheManagerMock = new Mock<ICacheManager>();
            webApiConfigurationsMock = new Mock<ChasmaWebApiConfigurations>();
        }

        /// <summary>
        /// Set up the tests before each case is run.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            Controller = new RemoteController(loggerMock.Object, controlServiceMock.Object, webApiConfigurationsMock.Object, cacheManagerMock.Object);
        }

        /// <summary>
        /// Cleans up the resources after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            loggerMock.Reset();
            controlServiceMock.Reset();
            cacheManagerMock.Reset();
            webApiConfigurationsMock.Reset();
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// null request is received.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenNullRequestIsReceived()
        {
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(null);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot get workflow runs.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// empty repository name is received.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenEmptyRepositoryNameIsReceived()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = string.Empty
            };
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(request);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository name is required.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// empty repository owner is received.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenEmptyRepositoryOwnerIsReceived()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = TestRepositoryName,
                RepositoryOwner = string.Empty
            };
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(request);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository owner is required.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// there is a failure getting run results.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenFailureToGetRunResults()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = TestRepositoryName,
                RepositoryOwner = TestUserName
            };
            string errorMessage = "Failed to get build results";
            List<WorkflowRunResult> workflowRuns = [];
            controlServiceMock.Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out workflowRuns, out errorMessage)).Returns(false);
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(request);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// there is a exception when attemtping to get run results.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenExceptionOccurs()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = TestRepositoryName,
                RepositoryOwner = TestUserName
            };
            string errorMessage = $"Error fetching workflow runs from {TestRepositoryName}. Check server logs for more information.";
            List<WorkflowRunResult> workflowRuns = [];
            controlServiceMock
                .Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out workflowRuns, out errorMessage))
                .Throws(new Exception("Exception getting run results."));
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(request);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends a successful response in the
        /// nominal case.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultsNominal()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = TestRepositoryName,
                RepositoryOwner = TestUserName
            };
            WorkflowRunResult result = new()
            {
                AuthorName = TestUserFullName,
                BranchName = "chasma",
            };
            string errorMessage = null;
            List<WorkflowRunResult> workflowRuns = [result];
            controlServiceMock
                .Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out workflowRuns, out errorMessage))
                .Returns(true);
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetGitHubWorkflowResults(request);
            GitHubWorkflowRunResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
            Assert.AreEqual(TestRepositoryName, response.RepositoryName);
            Assert.IsTrue(response.WorkflowRunResults.Count > 0);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when empty repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyRepositoryId()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot get branches.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an unknown repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithUnknownRepositoryId()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(new ConcurrentDictionary<string, string>());
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot get branches.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty pull request title is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyPullRequestTitle()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = string.Empty,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Pull request title must be populated. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty working branch name is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyWorkingBranchName()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = string.Empty,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Working branch name must be populated. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty destination branch name is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyDestinationBranchName()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = string.Empty,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Destination branch name must be populated. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty pull request body is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyPullRequestBody()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = string.Empty,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Pull request body message must be populated. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty repository owner is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyRepositoryOwner()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = "pull_request_body",
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(new ConcurrentDictionary<string, LocalGitRepository>());

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Owner of repository not found. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty repository name is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyRepositoryName()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = "pull_request_body",
                RepositoryName = string.Empty,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository repository = new()
            {
                UserId = 1,
                Id = request.RepositoryId,
                Owner = "chasma"
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [request.RepositoryId] = repository
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository name must be populated. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when a failure to create a pull request occurs. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithFailureToCreatePullRequest()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = "pull_request_body",
                RepositoryName = TestRepositoryName,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository repository = new()
            {
                UserId = 1,
                Id = request.RepositoryId,
                Owner = "chasma"
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [request.RepositoryId] = repository
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);

            int pullRequestId = 0;
            string prUrl = string.Empty;
            string timestamp = string.Empty;
            string errorMessage = string.Empty;

            controlServiceMock.Setup(i => i.TryCreatePullRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                out pullRequestId,
                out prUrl,
                out timestamp,
                out errorMessage))
                .Returns(false);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to create pull request for repo: {request.RepositoryName}. {errorMessage}", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when an exception occurs. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithUnexpectedException()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = "pull_request_body",
                RepositoryName = TestRepositoryName,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository repository = new()
            {
                UserId = 1,
                Id = request.RepositoryId,
                Owner = "chasma"
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [request.RepositoryId] = repository
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);

            int pullRequestId = 0;
            string prUrl = string.Empty;
            string timestamp = string.Empty;
            string errorMessage = string.Empty;

            controlServiceMock.Setup(i => i.TryCreatePullRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                out pullRequestId,
                out prUrl,
                out timestamp,
                out errorMessage))
                .Throws(new Exception("Exception generating pull request."));

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error creating pull request for repo: {request.RepositoryName}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends error response when null request is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithNullRequest()
        {
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(null);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot create pull request.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreatePullRequest(CreatePRRequest)"/> sends successful response in the nominal case. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestNominalCase()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                PullRequestTitle = "title",
                WorkingBranchName = "working_branch",
                DestinationBranchName = "destination_branch",
                PullRequestBody = "pull_request_body",
                RepositoryName = TestRepositoryName,
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository repository = new()
            {
                UserId = 1,
                Id = request.RepositoryId,
                Owner = "chasma"
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [request.RepositoryId] = repository
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);

            int pullRequestId = 0;
            string prUrl = "url.com";
            string timestamp = "1-01-2026";
            string errorMessage = null;

            controlServiceMock.Setup(i => i.TryCreatePullRequest(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                out pullRequestId,
                out prUrl,
                out timestamp,
                out errorMessage))
                .Returns(true);

            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
            Assert.AreEqual(pullRequestId, response.PullRequestId);
            Assert.AreEqual(prUrl, response.PullRequestUrl);
            Assert.AreEqual(timestamp, response.TimeStamp);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueWithNullRequest()
        {
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(null);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request is null. Cannot create issue.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the repository name is empty.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueWithEmptyRepositoryName()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = string.Empty,
            };
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository name must be populated. Cannot create issue.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the repository owner is empty.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueWithEmptyRepositoryOwner()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = string.Empty,
            };
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository owner must be populated. Cannot create issue.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the issue title is empty.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueWithEmptyIssueTitle()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = TestRepositoryName,
                Title = string.Empty,
            };
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Issue title must be populated. Cannot create issue.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the issue body is empty.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueWithEmptyIssueBody()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = TestRepositoryName,
                Title = "title",
                Body = string.Empty,
            };
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Issue body description must be populated. Cannot create issue.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response when the API fails to create issue.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueFailureToCreateIssueViaGitHubApi()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = TestRepositoryName,
                Title = "title",
                Body = "body_message",
            };

            int issueId = -1;
            string url = string.Empty;
            string errorMessage = "GitHub API failed to create issue";
            controlServiceMock.Setup(i => i.TryCreateIssue(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                out issueId,
                out url,
                out errorMessage))
                .Returns(false);
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends error response exception occurs during creation.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueHandlesExceptionProperly()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = TestRepositoryName,
                Title = "title",
                Body = "body_message",
            };

            int issueId = -1;
            string url = string.Empty;
            string errorMessage = string.Empty;
            controlServiceMock.Setup(i => i.TryCreateIssue(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out issueId,
                    out url,
                    out errorMessage))
                .Throws(new Exception("Could not create issue"));
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Exception occurred when creating GitHub issue. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RemoteController.CreateGitHubIssue(CreateGitHubIssueRequest)"/> sends successful response in the nominal case.
        /// </summary>
        [TestMethod]
        public void TestCreateGitHubIssueNominalCase()
        {
            CreateGitHubIssueRequest request = new()
            {
                RepositoryName = Guid.NewGuid().ToString(),
                RepositoryOwner = TestRepositoryName,
                Title = "title",
                Body = "body_message",
            };

            int issueId = 1;
            string url = "url.com";
            string errorMessage = null;
            controlServiceMock.Setup(i => i.TryCreateIssue(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    out issueId,
                    out url,
                    out errorMessage))
                .Returns(true);
            ActionResult<CreateGitHubIssueResponse> actionResult = Controller.CreateGitHubIssue(request);
            CreateGitHubIssueResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
            Assert.AreEqual(url, response.IssueUrl);
            Assert.AreEqual(issueId, response.IssueId);
        }
    }
}
