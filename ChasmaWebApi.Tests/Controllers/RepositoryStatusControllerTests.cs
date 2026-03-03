using ChasmaWebApi.Controllers;
using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
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
        /// The mocked internal API logger.
        /// </summary>
        private readonly Mock<ILogger<RepositoryStatusController>> loggerMock;

        /// <summary>
        /// The mocked internal cache manager.
        /// </summary>
        private readonly Mock<ICacheManager> cacheManagerMock;

        /// <summary>
        /// The mocked internal application control service.
        /// </summary>
        private readonly Mock<IApplicationControlService> controlServiceMock;

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
            controlServiceMock = new Mock<IApplicationControlService>();
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
            Controller = new RepositoryStatusController(loggerMock.Object, webApiConfigurations, controlServiceMock.Object, cacheManagerMock.Object);
        }

        /// <summary>
        /// Cleans up the resources after each test is ran.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            loggerMock.Reset();
            cacheManagerMock.Reset();
            controlServiceMock.Reset();
        }

        

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusSendsErrorResponseWithNullRequest()
        {
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(null);
            GitStatusResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitStatusResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("The repository identifier is null or empty.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when user cannot be found.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusFailsWithUnknownUser()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path",
            };

            ConcurrentDictionary<int, UserAccountModel> usersMapping = new();
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No user found in cache for user ID: {request.UserId}.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetRepoStatus(GitStatusRequest)"/> sends error response when the repository
        /// summary could not be retrieved.
        /// </summary>
        [TestMethod]
        public void TestGetRepoStatusWithFailureToGetStatus()
        {
            int userId = 1;
            GitStatusRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                UserId = userId
            };
            UserAccountModel user = new()
            {
                Id = userId,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new() { [user.Id] = user };
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);
            RepositorySummary summary = null;
            controlServiceMock.Setup(i => i.GetRepositoryStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(summary);
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(request);
            GitStatusResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            int userId = 1;
            GitStatusRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                UserId = userId,
            };
            UserAccountModel user = new()
            {
                Id = userId,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new() { [user.Id] = user };
            cacheManagerMock.Setup(i => i.Users).Returns(usersMapping);
            RepositorySummary summary = new()
            {
                CommitsAhead = 1,
                CommitsBehind = 2,
                CommitHash = "commit_hash",
            };
            controlServiceMock.Setup(i => i.GetRepositoryStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(summary);
            ActionResult<GitStatusResponse> actionResult = Controller.GetRepoStatus(request);
            GitStatusResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Cannot process request because the file name is not populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when user cannot be found.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionFailsWithUnknownUser()
        {
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path",
                UserId = 1,
            };

            ConcurrentDictionary<int, UserAccountModel> usersMapping = new();
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No user found in cache for user ID: {request.UserId}.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends error response when the staging operation fails.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionFailsToApplyStagingAction()
        {
            int userId = 1;
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path",
                UserId = userId,
            };
            UserAccountModel user = new()
            {
                Id = userId,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new() { [user.Id] = user };
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);
            List<RepositoryStatusElement>? statusElements = null;
            controlServiceMock.Setup(i => i.ApplyStagingAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>())).Returns(statusElements);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to apply staging action for repo ID: {request.RepoKey}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.ApplyStagingAction(ApplyStagingActionRequest)"/> sends success response when the staging operation succeeds.
        /// </summary>
        [TestMethod]
        public void TestApplyStagingActionNominalCase()
        {
            int userId = 1;
            ApplyStagingActionRequest request = new()
            {
                RepoKey = Guid.NewGuid().ToString(),
                FileName = "path",
                UserId = userId,
            };
            UserAccountModel user = new()
            {
                Id = userId,
                Email = "email",
                Name = "name",
                Password = "password",
                Salt = [],
                UserName = "username"
            };
            ConcurrentDictionary<int, UserAccountModel> usersMapping = new() { [user.Id] = user };
            List<RepositoryStatusElement>? statusElements = [new(), new()];
            cacheManagerMock.SetupGet(i => i.Users).Returns(usersMapping);
            controlServiceMock.Setup(i => i.ApplyStagingAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>())).Returns(statusElements);
            ActionResult<ApplyStagingActionResponse> actionResult = Controller.ApplyStagingAction(request);
            ApplyStagingActionResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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

            controlServiceMock
                .Setup(i => i.CommitChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Error occurred when committing changes."));

            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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

            controlServiceMock.Setup(i => i.CommitChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            ActionResult<GitCommitResponse> actionResult = Controller.CommitChanges(request);
            GitCommitResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            GitPushResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPushResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPushResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            controlServiceMock.Setup(i => i.TryPushChanges(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(false);

            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            controlServiceMock.Setup(i => i.TryPushChanges(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(true);

            ActionResult<GitPushResponse> actionResult = Controller.PushChanges(request);
            GitPushResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
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
            controlServiceMock
                .Setup(i => i.TryPullChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out errorMessage)) 
                .Returns(false);
            
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
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
            controlServiceMock
                .Setup(i => i.TryPullChanges(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), out errorMessage)) 
                .Returns(true);
            
            ActionResult<GitPullResponse> actionResult = Controller.PullChanges(request);
            GitPullResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends error response if
        /// the request is null.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffWithNullRequest()
        {
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(null);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request must be populated.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends error response if
        /// the repository identifier isn't populated.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffWithEmptyRepositoryId()
        {
            GitDiffRequest request = new();
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(request);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("The repository identifier is null or empty.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends error response if
        /// the working directory cannot be found.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffWithUnknownWorkingDirectory()
        {
            ConcurrentDictionary<string, string> workingDirectories = new();
            cacheManagerMock.Setup(i => i.WorkingDirectories).Returns(workingDirectories);
            string repositoryId = Guid.NewGuid().ToString();
            GitDiffRequest request = new() {RepositoryId = repositoryId};
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(request);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {repositoryId}", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends error response if
        /// the file path is empty.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffWithEmptyFilePath()
        {
            string repositoryId = Guid.NewGuid().ToString();
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repositoryId] = "working_directory",
            };
            cacheManagerMock.Setup(i => i.WorkingDirectories).Returns(workingDirectories);
            GitDiffRequest request = new() {RepositoryId = repositoryId};
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(request);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"The file path is null or empty.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends error response if
        /// diff cannot be generated.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffFailure()
        {
            string repositoryId = Guid.NewGuid().ToString();
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repositoryId] = "working_directory",
            };
            cacheManagerMock.Setup(i => i.WorkingDirectories).Returns(workingDirectories);
            string diffContent = string.Empty;
            string errorMessage = "Error generating diff.";
            controlServiceMock.Setup(i =>i.TryGetGitDiff(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                out diffContent,
                out errorMessage)
                ).Returns(false);
            string filePath = "path";
            GitDiffRequest request = new()
            {
                RepositoryId = repositoryId,
                FilePath = filePath,
            };
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(request);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to get git diff for repo ID: {repositoryId}, file path: {filePath}. Check server logs for more information.",  response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryStatusController.GetGitDiff(GitDiffRequest)"/> sends success response in
        /// the nominal case.
        /// </summary>
        [TestMethod]
        public void TestGetGitDiffNominalCase()
        {
            string repositoryId = Guid.NewGuid().ToString();
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repositoryId] = "working_directory",
            };
            cacheManagerMock.Setup(i => i.WorkingDirectories).Returns(workingDirectories);
            string diffContent = "diff_content";
            string errorMessage = string.Empty;
            controlServiceMock.Setup(i =>i.TryGetGitDiff(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                out diffContent,
                out errorMessage)
            ).Returns(true);
            string filePath = "path";
            GitDiffRequest request = new()
            {
                RepositoryId = repositoryId,
                FilePath = filePath,
            };
            ActionResult<GitDiffResponse> actionResult = Controller.GetGitDiff(request);
            GitDiffResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(diffContent, response.DiffContent);
        }
    }
}
