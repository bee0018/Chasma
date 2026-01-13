using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Tests.Factories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class testing the functionality of the <see cref="RepositoryConfigurationController"/>.
    /// </summary>
    [TestClass]
    public class RepositoryConfigurationControllerTests : ControllerTestBase<RepositoryConfigurationController>
    {
        /// <summary>
        /// The mocked internal logger for API testing.
        /// </summary>
        private readonly Mock<ILogger<RepositoryConfigurationController>> loggerMock;

        /// <summary>
        /// The mocked repository configuration manager.
        /// </summary>
        private readonly Mock<IRepositoryConfigurationManager> configurationManagerMock;

        /// <summary>
        /// The mocked cached manager.
        /// </summary>
        private readonly Mock<ICacheManager> cacheManagerMock;

        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private ApplicationDbContext applicationDbContext;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryConfigurationControllerTests"/> class.
        /// </summary>
        public RepositoryConfigurationControllerTests()
        {
            loggerMock = new Mock<ILogger<RepositoryConfigurationController>>();
            configurationManagerMock = new Mock<IRepositoryConfigurationManager>();
            cacheManagerMock = new Mock<ICacheManager>();
        }

        #endregion

        /// <summary>
        /// Set up the tests before each case is ran.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            CacheManagerFactory.SeedCacheManager(cacheManagerMock, TestRepositoryName, TestUserName);
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, applicationDbContext);
        }

        /// <summary>
        /// Cleans up the resources after each test.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            configurationManagerMock.Reset();
            cacheManagerMock.Reset();
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.GetLocalGitRepositories"/> method gets the
        /// local git repositories for a user.
        /// Note: Arbitrary user 1 is being tested so it is expected to have two repos only: TestRepository and Chasma.
        /// </summary>
        [TestMethod]
        public void TestGetLocalGitRepositories()
        {
            ActionResult<LocalRepositoriesInfoMessage> repoInfoMessageActionResult = Controller.GetLocalGitRepositories(1);
            LocalRepositoriesInfoMessage message = GetResponseFromHttpAction(repoInfoMessageActionResult, typeof(OkObjectResult));
            Assert.AreEqual(2, message.Repositories.Count);
            List<string> repoNames = message.Repositories.Select(i => i.Name).ToList();
            CollectionAssert.Contains(repoNames, TestRepositoryName);
            CollectionAssert.Contains(repoNames, "Chasma");
            CollectionAssert.DoesNotContain(repoNames, "KirbyGray");
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddLocalGitRepositories(int)"/> sends an error response if no local repositories
        /// cannot be found.
        /// </summary>
        [TestMethod]
        public void TestAddLocalGitRepositoriesSendsErrorResponseWhenNoReposFound()
        {
            List<LocalGitRepository> newRepositories = new();
            configurationManagerMock.Setup(x => x.TryAddLocalGitRepositories(It.IsAny<int>(), out newRepositories)).Returns(false);
            Task<ActionResult<AddLocalRepositoriesResponse>> addLocalGitRepositoriesTask = Controller.AddLocalGitRepositories(1);
            AddLocalRepositoriesResponse response = GetResponseFromHttpAction(addLocalGitRepositoriesTask, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("No new local git repositories found on this machine.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddLocalGitRepositories(int)"/> sends an successful in the nominal case.
        /// </summary>
        [TestMethod]
        public void TestAddLocalGitRepositoriesNominalCase()
        {
            applicationDbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(applicationDbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, applicationDbContext);
            List<LocalGitRepository> newRepositories = new()
            {
                CacheManagerFactory.CreateLocalGitRepository(1, TestRepositoryName, "chasma-bot"),
                CacheManagerFactory.CreateLocalGitRepository(2, "Chasma", "chasma-bot"),
            };
            ConcurrentDictionary<string, string> workingDirectories = new();
            foreach (LocalGitRepository repo in newRepositories)
            {
                workingDirectories.TryAdd(repo.Id, repo.Name);
            }
            cacheManagerMock.Setup(cacheManager => cacheManager.WorkingDirectories).Returns(workingDirectories);
            configurationManagerMock.Setup(x => x.TryAddLocalGitRepositories(It.IsAny<int>(), out newRepositories)).Returns(true);
            Task<ActionResult<AddLocalRepositoriesResponse>> addLocalGitRepositoriesTask = Controller.AddLocalGitRepositories(1);
            AddLocalRepositoriesResponse response = GetResponseFromHttpAction(addLocalGitRepositoriesTask, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);
            Assert.AreEqual(2, response.CurrentRepositories.Count);
            List<string> repoNames = response.CurrentRepositories.Select(r => r.Name).ToList();
            CollectionAssert.Contains(repoNames, TestRepositoryName);
            CollectionAssert.Contains(repoNames, "Chasma");
            TestDbContextFactory.DestroyDatabase(applicationDbContext);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteRepository(DeleteRepositoryRequest)"/> sends erorr response when null request is received.
        /// </summary>
        [TestMethod]
        public void TestDeleteRepositoryFailedWithNullRequest()
        {
            Task<ActionResult<DeleteRepositoryResponse>> responseTask = Controller.DeleteRepository(null);
            DeleteRepositoryResponse response = GetResponseFromHttpAction(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Null request was received. Cannot delete repo.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteRepository(DeleteRepositoryRequest)"/> sends erorr response when empty repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestDeleteRepositoryFailedWithEmptyRepositoryId()
        {
            DeleteRepositoryRequest request = new()
            {
                RepositoryId = string.Empty,
            };
            Task<ActionResult<DeleteRepositoryResponse>> responseTask = Controller.DeleteRepository(request);
            DeleteRepositoryResponse response = GetResponseFromHttpAction(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository identifier is required.", response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteRepository(DeleteRepositoryRequest)"/> sends erorr response when deletion error is reported.
        /// </summary>
        [TestMethod]
        public void TestDeleteRepositoryWithDeletionFailure()
        {
            DeleteRepositoryRequest request = new()
            {
                RepositoryId = "testRepoId",
            };

            List<LocalGitRepository> repositories = new();
            string errorMessage = "Error deleting repository.";
            configurationManagerMock.Setup(manager => manager.TryDeleteRepository(It.IsAny<string>(), It.IsAny<int>(), out repositories, out errorMessage)).Returns(false);
            Task<ActionResult<DeleteRepositoryResponse>> responseTask = Controller.DeleteRepository(request);
            DeleteRepositoryResponse response = GetResponseFromHttpAction(responseTask, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteRepository(DeleteRepositoryRequest)"/> sends successful response on nominal case.
        /// </summary>
        [TestMethod]
        public void TestDeleteRepositoryNominalCase()
        {
            LocalGitRepository testRepo = new LocalGitRepository
            {
                Id = Guid.NewGuid().ToString(),
                UserId = 1,
                Name = TestRepositoryName,
                Owner = TestUserFullName,
                Url = "url",
            };

            RepositoryModel repo = new()
            {
                Id = testRepo.Id,
                Name = TestRepositoryName,
                Owner = TestUserFullName,
                Url = "url",
                UserId = 1,
            };

            WorkingDirectoryModel workingDirectory = new()
            {
                RepositoryId = testRepo.Id,
                WorkingDirectory = "path_xyz"
            };
            applicationDbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(applicationDbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            applicationDbContext.Repositories.Add(repo);
            applicationDbContext.WorkingDirectories.Add(workingDirectory);
            applicationDbContext.SaveChanges();

            DeleteRepositoryRequest request = new()
            {
                RepositoryId = testRepo.Id,
                UserId = 1
            };

            List<LocalGitRepository> repositories = new();
            string errorMessage = string.Empty;
            configurationManagerMock.Setup(manager => manager.TryDeleteRepository(It.IsAny<string>(), It.IsAny<int>(), out repositories, out errorMessage)).Returns(true);
            
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, applicationDbContext);
            Task<ActionResult<DeleteRepositoryResponse>> responseTask = Controller.DeleteRepository(request);
            DeleteRepositoryResponse response = GetResponseFromHttpAction(responseTask, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(null, response.ErrorMessage);

            List<string> repoIds = cacheManagerMock.Object.Repositories.Values.Select(i => i.Id).ToList();
            List<string> workingDirectoryKeys = cacheManagerMock.Object.Repositories.Keys.ToList();
            CollectionAssert.DoesNotContain(repoIds, testRepo.Id);
            CollectionAssert.DoesNotContain(workingDirectoryKeys, testRepo.Id);
            TestDbContextFactory.DestroyDatabase(applicationDbContext);
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the request is null.
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
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the repository
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
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteBranch(DeleteBranchRequest)"/> sends error response when the
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
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteBranch(DeleteBranchRequest)"/> sends error response when repository
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
            configurationManagerMock
                .Setup(i => i.TryDeleteBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage))
                .Returns(false);
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.DeleteBranch(DeleteBranchRequest)"/> sends successful response
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
            configurationManagerMock
                .Setup(i => i.TryDeleteBranch(It.IsAny<string>(), It.IsAny<string>(), out errorMessage))
                .Returns(true);
            ActionResult<DeleteBranchResponse> actionResult = Controller.DeleteBranch(request);
            DeleteBranchResponse response = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }
    }
}
