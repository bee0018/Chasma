using ChasmaWebApi.Controllers;
using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Test class representing the branch controller tests for database CRUD operations.
    /// </summary>
    [TestClass]
    public class BranchControllerTests : ControllerTestBase<BranchController>
    {
        /// <summary>
        /// The mocked internal logger for API testing.
        /// </summary>
        private readonly Mock<ILogger<BranchController>> loggerMock;

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

        public BranchControllerTests()
        {
            controlServiceMock = new Mock<IApplicationControlService>();
            loggerMock = new Mock<ILogger<BranchController>>();
            cacheManagerMock = new Mock<ICacheManager>();
            webApiConfigurationsMock = new Mock<ChasmaWebApiConfigurations>();
        }

        /// <summary>
        /// Set up the tests before each case is run.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            Controller = new BranchController(loggerMock.Object, controlServiceMock.Object, cacheManagerMock.Object, webApiConfigurationsMock.Object);
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
        /// Tests that the <see cref="BranchController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestDeleteBranchWithNullRequest()
        {
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(null);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Request must be populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the repository
        /// identifier is empty.
        /// </summary>
        [TestMethod]
        public void TestDeleteBranchWithEmptyRepositoryId()
        {
            DeleteBranchRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Empty repository identifier received. Field must be populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the
        /// branch name is empty.
        /// </summary>
        [TestMethod]
        public void TestDeleteBranchWithEmptyBranchName()
        {
            DeleteBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                BranchName = string.Empty,
            };
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Empty branch name received. Field must be populated.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.DeleteBranch(DeleteBranchRequest)"/> sends error response when repository
        /// fails to delete the branch.
        /// </summary>
        [TestMethod]
        public void TestDeleteBranchWithFailureToDeleteBranch()
        {
            DeleteBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                BranchName = "branch_name",
            };

            string errorMessage = "Failed to delete branch.";
            controlServiceMock
                .Setup(i => i.TryDeleteExistingBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage))
                .Returns(false);
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.DeleteBranch(DeleteBranchRequest)"/> sends successful response
        /// in the nominal case of deleting a branch.
        /// </summary>
        [TestMethod]
        public void TestDeleteBranchNominalCase()
        {
            DeleteBranchRequest request = new()
            {
                RepositoryId = Guid.NewGuid().ToString(),
                BranchName = "branch_name",
            };
            string errorMessage = null;
            controlServiceMock
                .Setup(i => i.TryDeleteExistingBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage))
                .Returns(true);
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when null request is received.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithNullRequest()
        {
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(null);
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot checkout branch.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when empty repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestCheckoutBranchWithEmptyRepositoryId()
        {
            GitCheckoutRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot checkout branch.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when an unknown repository identifier is received.
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
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot checkout branch.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when a failure to checkout branch occurs.
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
            controlServiceMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(false);
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Failed to checkout branch to repo: {request.RepositoryId}. {errorMessage}", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends error response when an unexpected exception occurs.
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
            controlServiceMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Throws(new Exception("Exception checking out branch."));
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error checking out branch to repo: {request.RepositoryId}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.CheckoutBranch(GitCheckoutRequest)"/> sends successful response in nominal case.
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
            controlServiceMock.Setup(i => i.TryCheckoutBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage)).Returns(true);
            ActionResult<GitCheckoutResponse> actionResult = Controller.CheckoutBranch(request);
            GitCheckoutResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.GetBranches(GitBranchRequest)"/> sends error response when null request is received. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithNullRequest()
        {
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(null);
            GitBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request received. Cannot get branches.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.GetBranches(GitBranchRequest)"/> sends error response when empty repository identifier is received. 
        /// </summary>
        [TestMethod]
        public void TestGetBranchesWithEmptyRepositoryId()
        {
            GitBranchRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Repository identifier must be populated. Cannot get branches.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.GetBranches(GitBranchRequest)"/> sends error response when unknown repository identifier is received. 
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
            GitBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"No working directory found in cache for {request.RepositoryId}. Cannot get branches.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.GetBranches(GitBranchRequest)"/> sends error response when there is failure to get branches via git operations. 
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
            controlServiceMock.Setup(i => i.GetAllBranchesForRepository(It.IsAny<string>())).Throws(new Exception("Exception getting branches."));
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual($"Error getting branches for repo: {request.RepositoryId}. Check server logs for more information.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="BranchController.GetBranches(GitBranchRequest)"/> sends successful response in the nominal case. 
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
            controlServiceMock.Setup(i => i.GetAllBranchesForRepository(It.IsAny<string>())).Returns(branchNames);
            ActionResult<GitBranchResponse> actionResult = Controller.GetBranches(request);
            GitBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
            Assert.IsTrue(response.BranchNames.Count == branchNames.Count);
        }
    }
}
