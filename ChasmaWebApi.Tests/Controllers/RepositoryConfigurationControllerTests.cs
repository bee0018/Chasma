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

        /// <summary>
        /// Tests the nominal case of the <see cref="RepositoryConfigurationController.GetIgnoredRepositories(int)"/> getting the
        /// ignored repository details associated with the specified user.
        /// </summary>
        [TestMethod]
        public void TestGetIgnoredRepositoriesNominalCase()
        {
            LocalGitRepository testRepo = new()
            {
                Id = "testId1",
                Name = "testName1",
                IsIgnored = true,
                Owner = "chasma",
                Url = "url1.com",
                UserId = 1,
            };
            LocalGitRepository testRepo2 = new()
            {
                Id = "testId2",
                Name = "testName2",
                IsIgnored = true,
                Owner = "chasma",
                Url = "url2.com",
                UserId = 1,
            };
            LocalGitRepository testRepo3 = new()
            {
                Id = "testId3",
                Name = "testName3",
                IsIgnored = false,
                Owner = "chasma",
                Url = "url3.com",
                UserId = 1,
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [testRepo.Id] = testRepo,
                [testRepo2.Id] = testRepo2,
                [testRepo3.Id] = testRepo3
            };
            
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);
            ActionResult<GetIgnoredRepositoriesMessage> actionResult = Controller.GetIgnoredRepositories(1);
            GetIgnoredRepositoriesMessage message = GetResponseFromHttpAction(actionResult, typeof(OkObjectResult));
            Assert.AreEqual(2, message.IgnoredRepositories.Count);
            CollectionAssert.Contains(message.IgnoredRepositories, $"{testRepo.Name}:{testRepo.Id}");
            CollectionAssert.Contains(message.IgnoredRepositories, $"{testRepo2.Name}:{testRepo2.Id}");
            CollectionAssert.DoesNotContain(message.IgnoredRepositories, $"{testRepo3.Name}:{testRepo3.Id}");
        }

        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response when a null request is received.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithNullRequest()
        {
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(null);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request must not be empty.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response when an empty repository identifier is received.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithEmptyRepositoryId()
        {
            IgnoreRepositoryRequest request = new();
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository identifier is required.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response the working directory cannot be found for the repository.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithWorkingDirectoryCannotBeFound()
        {
            ConcurrentDictionary<string, string> workingDirectories = new();
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);
            IgnoreRepositoryRequest request = new() {RepositoryId = "repoId"};
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("No working directory was found for the specified repository.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response the local git repository cannot be found.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithRepositoryCannotBeFound()
        {
            string repoId = Guid.NewGuid().ToString();
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repoId] = "workingDirectory",
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            ConcurrentDictionary<string, LocalGitRepository> repositories = new();
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);
            
            IgnoreRepositoryRequest request = new() {RepositoryId = repoId};
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("No repository was found in cache.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response when the database cannot find the repository model.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithDatabaseFailureToIgnoreUnknownRepository()
        {
            ApplicationDbContext dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, dbContext);
            string repoId = Guid.NewGuid().ToString();
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repoId] = "workingDirectory",
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository testRepo = new()
            {
                Id = repoId,
                Name = "testRepo",
                IsIgnored = false,
                Owner = "chasma",
                Url = "url1.com",
                UserId = 1,
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [testRepo.Id] = testRepo,
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);
            
            IgnoreRepositoryRequest request = new() {RepositoryId = repoId};
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Cannot ignore repository because it does not exist in the database.", response.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// error response when the database does not update any repository models.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryWithDatabaseFailureToIgnoreRepository()
        {
            ApplicationDbContext dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, dbContext);
            string repoId = "testRepo1234";
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repoId] = "workingDirectory",
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository testRepo = new()
            {
                Id = repoId,
                Name = "testRepo",
                IsIgnored = true,
                Owner = "chasma",
                Url = "url1.com",
                UserId = 1,
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [testRepo.Id] = testRepo,
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);
            
            IgnoreRepositoryRequest request = new()
            {
                RepositoryId = repoId,
                UserId = 100,
                IsIgnored = true,
            };
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Failed to save changes to database. Check server logs for more information.", response.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.IgnoreRepository(IgnoreRepositoryRequest)"/> sends
        /// successful response when the database updates the repository models.
        /// </summary>
        [TestMethod]
        public void TestIgnoreRepositoryNominalCase()
        {
            ApplicationDbContext dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, dbContext);
            string repoId = "testRepo1234";
            ConcurrentDictionary<string, string> workingDirectories = new()
            {
                [repoId] = "workingDirectory",
            };
            cacheManagerMock.SetupGet(i => i.WorkingDirectories).Returns(workingDirectories);

            LocalGitRepository testRepo = new()
            {
                Id = repoId,
                Name = "testRepo",
                IsIgnored = false,
                Owner = "chasma",
                Url = "url1.com",
                UserId = 1,
            };
            ConcurrentDictionary<string, LocalGitRepository> repositories = new()
            {
                [testRepo.Id] = testRepo,
            };
            cacheManagerMock.SetupGet(i => i.Repositories).Returns(repositories);
            
            IgnoreRepositoryRequest request = new()
            {
                RepositoryId = repoId,
                UserId = 100,
                IsIgnored = false,
            };
            Task<ActionResult<IgnoreRepositoryResponse>> ignoredRepoTask = Controller.IgnoreRepository(request);
            IgnoreRepositoryResponse response = GetResponseFromHttpAction(ignoredRepoTask, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddGitRepository(AddGitRepositoryRequest)"/> sends
        /// an error response when the request is null.
        /// </summary>
        [TestMethod]
        public void TestAddGitRepositoryWithNullRequest()
        {
            Task<ActionResult<AddGitRepositoryResponse>> task = Controller.AddGitRepository(null);
            AddGitRepositoryResponse response = GetResponseFromHttpAction(task, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Request must be populated.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddGitRepository(AddGitRepositoryRequest)"/> sends
        /// an error response when the repository path is empty.
        /// </summary>
        [TestMethod]
        public void TestAddGitRepositoryWithEmptyRepositoryPath()
        {
            AddGitRepositoryRequest request = new();
            Task<ActionResult<AddGitRepositoryResponse>> task = Controller.AddGitRepository(request);
            AddGitRepositoryResponse response = GetResponseFromHttpAction(task, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Repository path is required.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddGitRepository(AddGitRepositoryRequest)"/> sends
        /// an error response when the user requesting is unknown.
        /// </summary>
        [TestMethod]
        public void TestAddGitRepositoryWithUnknownUser()
        {
            ConcurrentDictionary<int, UserAccountModel> users = new();
            cacheManagerMock.Setup(i => i.Users).Returns(users);
            
            AddGitRepositoryRequest request = new()
            {
                RepositoryPath = "test_path",
                UserId = 10000,
            };
            Task<ActionResult<AddGitRepositoryResponse>> task = Controller.AddGitRepository(request);
            AddGitRepositoryResponse response = GetResponseFromHttpAction(task, typeof(BadRequestObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual("Invalid request. Could not find user.", response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddGitRepository(AddGitRepositoryRequest)"/> sends
        /// an error response when the system fails to add the repository.
        /// </summary>
        [TestMethod]
        public void TestAddGitRepositoryWithFailureToAddRepository()
        {
            UserAccountModel user = new()
            {
                Id = 1,
                Email = TestUserEmail,
                Name = TestUserFullName,
                Password = TestUserPassword,
                UserName = TestUserName,
                Salt = [1, 2, 3]
            };
            ConcurrentDictionary<int, UserAccountModel> users = new()
            {
                [user.Id] = user,
            };
            cacheManagerMock.Setup(i => i.Users).Returns(users);

            LocalGitRepository testRepo = null;
            string errorMessage = "Could not add repository";
            configurationManagerMock.Setup(i => i.TryAddGitRepository(
                It.IsAny<string>(),
                It.IsAny<int>(),
                out testRepo,
                out errorMessage)
            ).Returns(false);
            
            AddGitRepositoryRequest request = new()
            {
                RepositoryPath = "test_path",
                UserId = user.Id,
            };
            Task<ActionResult<AddGitRepositoryResponse>> task = Controller.AddGitRepository(request);
            AddGitRepositoryResponse response = GetResponseFromHttpAction(task, typeof(OkObjectResult));
            Assert.IsTrue(response.IsErrorResponse);
            Assert.AreEqual(errorMessage, response.ErrorMessage);
        }
        
        /// <summary>
        /// Tests that the <see cref="RepositoryConfigurationController.AddGitRepository(AddGitRepositoryRequest)"/> sends
        /// a success response when the system adds the repository.
        /// </summary>
        [TestMethod]
        public void TestAddGitRepositoryNominalCase()
        {
            UserAccountModel user = new()
            {
                Id = 1,
                Email = TestUserEmail,
                Name = TestUserFullName,
                Password = TestUserPassword,
                UserName = TestUserName,
                Salt = [1, 2, 3]
            };
            ConcurrentDictionary<int, UserAccountModel> users = new()
            {
                [user.Id] = user,
            };
            cacheManagerMock.Setup(i => i.Users).Returns(users);

            LocalGitRepository testRepo = new LocalGitRepository
            {
                Id = Guid.NewGuid().ToString(),
                Name = TestRepositoryName,
                UserId = user.Id,
                Owner = TestUserFullName,
                Url = "test_url",
            };
            string errorMessage = null;
            configurationManagerMock.Setup(i => i.TryAddGitRepository(
                It.IsAny<string>(),
                It.IsAny<int>(),
                out testRepo,
                out errorMessage)
            ).Returns(true);
            
            AddGitRepositoryRequest request = new()
            {
                RepositoryPath = "test_path",
                UserId = user.Id,
            };
            applicationDbContext = TestDbContextFactory.CreateApplicationDbContext();
            Controller = new RepositoryConfigurationController(loggerMock.Object, configurationManagerMock.Object, cacheManagerMock.Object, applicationDbContext);
            Task<ActionResult<AddGitRepositoryResponse>> task = Controller.AddGitRepository(request);
            AddGitRepositoryResponse response = GetResponseFromHttpAction(task, typeof(OkObjectResult));
            Assert.IsFalse(response.IsErrorResponse);
            Assert.AreEqual(testRepo.Id, response.Repository.Id);
            Assert.AreEqual(testRepo.Name, response.Repository.Name);
            Assert.AreEqual(testRepo.Url, response.Repository.Url);
            Assert.AreEqual(testRepo.UserId, response.Repository.UserId);
            Assert.AreEqual(testRepo.Owner, response.Repository.Owner);
            Assert.AreEqual(testRepo.IsIgnored, response.Repository.IsIgnored);
            TestDbContextFactory.DestroyDatabase(applicationDbContext);
        }
    }
}
