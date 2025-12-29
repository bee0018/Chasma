using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
using ChasmaWebApi.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class testing the functionality of the <see cref="RepositoryStatusController"/>.
    /// </summary>
    [TestClass]
    public class RepositoryStatusControllerTests : ControllerTestBase<RepositoryStatusController>
    {
        /// <summary>
        /// The API configurations options.
        /// </summary>
        private readonly ChasmaWebApiConfigurations webApiConfigurations;

        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// The mocked internal API logger.
        /// </summary>
        private readonly Mock<ILogger<RepositoryStatusController>> loggerMock;

        /// <summary>
        /// The mocked internal repository status manager.
        /// </summary>
        private readonly Mock<IRepositoryStatusManager> statusManagerMock;

        /// <summary>
        /// The mocked internal cache manager.
        /// </summary>
        private readonly Mock<ICacheManager> cacheManagerMock;

        /// <summary>
        /// The configuration file location.
        /// </summary>
        private const string ConfigFilePath = "config.xml";

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryConfigurationControllerTests"/> class.
        /// </summary>
        public RepositoryStatusControllerTests()
        {
            statusManagerMock = new Mock<IRepositoryStatusManager>();
            cacheManagerMock = new Mock<ICacheManager>();
            loggerMock = new Mock<ILogger<RepositoryStatusController>>();
            webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(ConfigFilePath)!;
        }

        #endregion

        /// <summary>
        /// Sets up the resources before each unit test is ran.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            Controller = new RepositoryStatusController(loggerMock.Object, webApiConfigurations, statusManagerMock.Object, cacheManagerMock.Object, applicationDbContext);
        }

        /// <summary>
        /// Cleans up the resources after each test is ran.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            loggerMock.Reset();
            statusManagerMock.Reset();
            cacheManagerMock.Reset();
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// null request is received.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenNullRequestIsReceived()
        {
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(null);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot get workflow runs.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
        /// empty repository name is received.
        /// </summary>
        [TestMethod]
        public void TestGetChasmaWorkflowResultSendsErrorResponseWhenEmptyRepositoryNameIsReceived()
        {
            GetWorkflowResultsRequest request = new()
            {
                RepositoryName = string.Empty
            };
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(request);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository name is required.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
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
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(request);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository owner is required.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
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
            List<WorkflowRunResult> workflowRuns = new();
            statusManagerMock.Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out workflowRuns, out errorMessage)).Returns(false);
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(request);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends error response when
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
            List<WorkflowRunResult> workflowRuns = new();
            statusManagerMock
                .Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out workflowRuns, out errorMessage))
                .Throws(new Exception("Exception getting run results."));
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(request);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetChasmaWorkflowResults(GetWorkflowResultsRequest)"/> sends a successful response in the
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
            statusManagerMock
                .Setup(i => i.TryGetWorkflowRunResults(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out workflowRuns, out errorMessage))
                .Returns(true);
            ActionResult<GitHubWorkflowRunResponse> actionResult = Controller.GetChasmaWorkflowResults(request);
            GitHubWorkflowRunResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
            Assert.AreEqual(TestRepositoryName, response.RepositoryName);
            Assert.IsTrue(response.WorkflowRunResults.Count > 0);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusSendsErrorResponseWithNullRequest()
        {
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(null);
            GitStatusResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request must be populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when the repository identifier is empty.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusWithEmptyRepositoryId()
        {
            GitStatusRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(request);
            GitStatusResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("The repository identifier is null or empty.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when the repository
        /// summary could not be retrieved.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusWithFailureToGetStatus()
        {
            GitStatusRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            RepositorySummary summary = null;
            statusManagerMock.Setup(i => i.GetRepositoryStatus(It.IsAny<string>())).Returns(summary);
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(request);
            GitStatusResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to get repository status for repo ID: {request.RepositoryId}", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends success response when 
        /// repository summary is retrieved.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusNominalCase()
        {
            GitStatusRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            RepositorySummary summary = new()
            {
                CommitsAhead = 1,
                CommitsBehind = 2,
                CommitHash = "commit_hash",
            };
            statusManagerMock.Setup(i => i.GetRepositoryStatus(It.IsAny<string>())).Returns(summary);
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(request);
            GitStatusResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
            Assert.IsTrue(response.CommitsBehind > 0);
            Assert.IsTrue(response.CommitsAhead > 0);
            Assert.IsFalse(string.IsNullOrEmpty(response.CommitHash));
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestGetApplyStagingActionSendsErrorResponseWithNullRequest()
        {
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(null);
            ApplyStagingActionResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request must be populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when the repository key is empty.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionWithEmptyRepositoryKey()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = string.Empty,
            };
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Cannot process request because the repository key is not populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when the file name is empty.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionWithEmptyFileName()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = string.Empty
            };
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Cannot process request because the file name is not populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when the staging operation fails.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionFailsToApplyStagingAction()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path"
            };
            List<RepositoryStatusElement>? statusElements = null;
            statusManagerMock.Setup(i => i.ApplyStagingAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(statusElements);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to apply staging action for repo ID: {request.RepoKey}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends success response when the staging operation succeeds.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionNominalCase()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path"
            };
            List<RepositoryStatusElement>? statusElements = [new(), new()];
            statusManagerMock.Setup(i => i.ApplyStagingAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(statusElements);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
            Assert.IsTrue(response.StatusElements.Count > 0);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when a null request is received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithNullRequest()
        {
            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(null);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when empty repository identifier received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithEmptyRepositoryId()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = string.Empty
            };
            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when empty email received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithEmptyEmail()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = string.Empty,
            };
            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Email must be populated for commit signature. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when empty commit message received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithEmptyCommitMessage()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                CommitMessage = string.Empty
            };
            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Commit message cannot be empty. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when unknown repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithUnknownRepositoryId()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                CommitMessage = "commit_message"
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when unknown user identifier is received.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithUnknownUserId()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                CommitMessage = "commit_message",
                UserId = 1,
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ConcurrentDictionary<int, UserAccountModel> usersMapping = new();
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);

            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No user found in cache for user ID: {request.UserId}. Cannot commit changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends error response when commit changes operation
        /// fails.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesWithCommitChangesFailure()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                CommitMessage = "commit_message",
                UserId = 1,
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            UserAccountModel user = new()
            {
                Id = 1,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new();
            usersMapping[user.Id] = user;
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);

            statusManagerMock
                .Setup(i => i.CommitChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Error occurred when committing changes."));

            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error committing changes to repo: {request.RepositoryId}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CommitChanges(GitCommitRequest)"/> sends success response in the nominal case.
        /// </summary>
        [TestMethod]
        public void TestCommitChangesNominalCase()
        {
            GitCommitRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                CommitMessage = "commit_message",
                UserId = 1,
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            UserAccountModel user = new()
            {
                Id = 1,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new();
            usersMapping[user.Id] = user;
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);

            statusManagerMock.Setup(i => i.CommitChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PushChanges(GitPushRequest)"/> sends error response when null request is received.
        /// </summary>
        [TestMethod]
        public void TestPushChangesWithNullRequest()
        {
            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(null);
            GitPushResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot push changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends error response when empty repository identifier received.
        /// </summary>
        [TestMethod]
        public void TestPushChangesWithEmptyRepositoryId()
        {
            GitPushRequest request = new()
            {
                RepositoryId = string.Empty
            };
            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot push changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PushChanges(GitPushRequest)"/> sends error response when an unknown repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestPushChangesWithUnknownRepositoryId()
        {
            GitPushRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot push changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PushChanges(GitPushRequest)"/> sends error response when failure to push occurs.
        /// </summary>
        [TestMethod]
        public void TestPushChangesWithFailureToPush()
        {
            GitPushRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            string errorMessage = string.Empty;
            statusManagerMock.Setup(i => i.TryPushChanges(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(false);

            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to push changes to repo: {request.RepositoryId}. {errorMessage}", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PushChanges(GitPushRequest)"/> sends success response in nominal case.
        /// </summary>
        [TestMethod]
        public void TestPushChangesNominalCase()
        {
            GitPushRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            string errorMessage = string.Empty;
            statusManagerMock.Setup(i => i.TryPushChanges(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(true);

            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithNullRequest()
        {
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(null);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot pull changes.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when the request contains
        /// empty repository identifier.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithEmptyRepositoryId()
        {
            GitPullRequest request = new()
            {
                RepositoryId = string.Empty
            };
            ActionResult<GitPullResponse> actionResult  = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot pull changes.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when the request contains
        /// empty email.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithEmptyEmail()
        {
            GitPullRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = string.Empty
            };
            ActionResult<GitPullResponse> actionResult  = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("User's email must be populated. Cannot pull changes.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when the request contains
        /// unknown repository identifier.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithUnknownRepositoryId()
        {
            GitPullRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot pull changes.",  response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when the request contains
        /// unknown user identifier.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithUnknownUserId()
        {
            GitPullRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                UserId = 1,
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[request.RepositoryId] = "working_directory";
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            ConcurrentDictionary<int, UserAccountModel> userAccounts = new();
            cacheManagerMock.SetupGet(i => i.Users).Returns(userAccounts);
            
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No user found in cache for user ID: {request.UserId}. Cannot pull changes.",  response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends an error response when repository fails to pull changes.
        /// </summary>
        [TestMethod]
        public void TestPullChangesWithFailureToPullChangesForRepo()
        {
            GitPullRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                UserId = 1,
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            ConcurrentDictionary<int, UserAccountModel> userAccounts = new();
            userAccounts[request.UserId] = new UserAccountModel
            {
                Id = 1,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            cacheManagerMock.SetupGet(i => i.Users).Returns(userAccounts);
            
            string errorMessage = string.Empty;
            statusManagerMock
                .Setup(i => i.TryPullChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out errorMessage)) 
                .Returns(false);
            
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to pull changes to repo: {request.RepositoryId}. {errorMessage}",  response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.PullChanges(GitPullRequest)"/> sends successful response for the nominal case.
        /// </summary>
        [TestMethod]
        public void TestPullChangesNominalCase()
        {
            GitPullRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                Email = "email",
                UserId = 1,
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            UserAccountModel user = new UserAccountModel
            {
                Id = 1,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> userAccounts = new()
            {
                [user.Id] = user
            };
            cacheManagerMock.SetupGet(i => i.Users).Returns(userAccounts);
            
            string errorMessage = string.Empty;
            statusManagerMock
                .Setup(i => i.TryPullChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out errorMessage)) 
                .Returns(true);
            
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when null request is received.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithNullRequest()
        {
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(null);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot checkout branch.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when empty repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithEmptyRepositoryId()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot checkout branch.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when an unknown repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithUnknownRepositoryId()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(new ConcurrentDictionary<string, string>());
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot checkout branch.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when a failure to checkout branch occurs.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithCheckoutFailure()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            string errorMessage = "Failed to pull checkout branch";
            statusManagerMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(false);
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to checkout branch to repo: {request.RepositoryId}. {errorMessage}", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when an unexpected exception occurs.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithUnexpectedException()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            string errorMessage = "Failed to pull checkout branch";
            statusManagerMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Throws(new Exception("Exception checking out branch."));
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error checking out branch to repo: {request.RepositoryId}. Check server logs for more information.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CheckoutBranch(GitCheckoutRequest)"/> sends successful response in nominal case.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchNominalCase()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            
            string errorMessage = null;
            statusManagerMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(true);
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetBranches(GitBranchRequest)"/> sends error response when null request is received. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithNullRequest()
        {
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(null);
            GitBranchResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot get branches.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetBranches(GitBranchRequest)"/> sends error response when empty repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithEmptyRepositoryId()
        {
            GitBranchRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot get branches.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetBranches(GitBranchRequest)"/> sends error response when unknown repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithUnknownRepositoryId()
        {
            GitBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(new ConcurrentDictionary<string, string>());
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot get branches.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetBranches(GitBranchRequest)"/> sends error response when there is failure to get branches via git operations. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithFailureToGetBranches()
        {
            GitBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            statusManagerMock.Setup(i => i.GetAllBranches(It.IsAny<string>())).Throws(new Exception("Exception getting branches."));
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error getting branches for repo: {request.RepositoryId}. Check server logs for more information.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetBranches(GitBranchRequest)"/> sends successful response in the nominal case. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesNominalCase()
        {
            GitBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
            };

            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [request.RepositoryId] = "working_directory"
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            List<string> branchNames = ["chasma", "unit", "test"];
            statusManagerMock.Setup(i => i.GetAllBranches(It.IsAny<string>())).Returns(branchNames);
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
            Assert.IsTrue(response.BranchNames.Count == branchNames.Count);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when null request is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithNullRequest()
        {
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(null);
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when empty repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestCreatePullRequestWithEmptyRepositoryId()
        {
            CreatePRRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot get branches.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an unknown repository identifier is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot get branches.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty pull request title is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Pull request title must be populated. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty working branch name is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Working branch name must be populated. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty destination branch name is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Destination branch name must be populated. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty pull request body is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Pull request body message must be populated. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty repository owner is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Owner of repository not found. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an empty repository name is received. 
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
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository name must be populated. Cannot create pull request.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when a failure to create a pull request occurs. 
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

            statusManagerMock.Setup(i => i.TryCreatePullRequest(
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
                              out errorMessage)
                ).Returns(false);
            
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to create pull request for repo: {request.RepositoryName}. {errorMessage}", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends error response when an exception occurs. 
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

            statusManagerMock.Setup(i => i.TryCreatePullRequest(
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
                              out errorMessage)
                ).Throws(new Exception("Exception generating pull request."));
            
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error creating pull request for repo: {request.RepositoryName}. Check server logs for more information.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.CreatePullRequest(CreatePRRequest)"/> sends successful response in the nominal case. 
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

            statusManagerMock.Setup(i => i.TryCreatePullRequest(
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
                              out errorMessage)
                ).Returns(true);
            
            ActionResult<CreatePRResponse> actionResult = Controller.CreatePullRequest(request);
            CreatePRResponse response = ExtractActionResultInnerResponseFromActionResult(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
            Assert.AreEqual(pullRequestId, response.PullRequestId);
            Assert.AreEqual(prUrl, response.PullRequestUrl);
            Assert.AreEqual(timestamp, response.TimeStamp);
        }
    }
}
