using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
using ChasmaWebApi.Tests.Factories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChasmaWebApi.Tests.Controllers
{
    /// <summary>
    /// Class testing the functionality of the <see cref="UserController"/> class.
    /// </summary>
    [TestClass]
    public class UserControllerTests : ControllerTestBase<UserController>
    {
        /// <summary>
        /// The database context used for testing.
        /// </summary>
        private ApplicationDbContext dbContext;

        /// <summary>
        /// The mocked internal logger for API testing.
        /// </summary>
        private readonly Mock<ILogger<UserController>> loggerMock;

        /// <summary>
        /// The mocked password utility for API testing.
        /// </summary>
        private readonly Mock<IPasswordUtility> passwordUtilityMock;

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UserControllerTests"/> class.
        /// </summary>
        public UserControllerTests()
        {
            loggerMock = new Mock<ILogger<UserController>>();
            passwordUtilityMock = new Mock<IPasswordUtility>();
        }

        #endregion

        /// <summary>
        /// Sets up resources before each test.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
        }

        /// <summary>
        /// Disposes of test resources after each test.
        /// </summary>
        [TestCleanup]
        public void CleanupTests()
        {
            passwordUtilityMock.Reset();
            loggerMock.Reset();
        }

        /// <summary>
        /// Tests that the <see cref="UserController.Login(LoginRequest)"/> sends a login error response is sent when the request is null.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestLoginFailsWithNullLoginRequest()
        {
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(null);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(loginResponse.IsErrorResponse);
            Assert.AreEqual("Request was null. Cannot login user.", loginResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.Login(LoginRequest)"/> sends a login error response is sent when the request has an empty username field.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestLoginFailsWithEmptyUsername()
        {
            LoginRequest request = new LoginRequest
            {
                UserName = string.Empty,
            };
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(request);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(loginResponse.IsErrorResponse);
            Assert.AreEqual("Username is empty. Cannot login user.", loginResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.Login(LoginRequest)"/> sends a login error response is sent when the request has an empty password field.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestLoginFailsWithEmptyPassword()
        {
            LoginRequest request = new LoginRequest
            {
                UserName = "user1",
                Password = string.Empty
            };
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(request);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(loginResponse.IsErrorResponse);
            Assert.AreEqual("Password is empty. Cannot login user.", loginResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.Login(LoginRequest)"/> sends a login error response is sent when the request has a username that does not exist.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestLoginFailsWithUnknownUser()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            LoginRequest request = new LoginRequest
            {
                UserName = "user1",
                Password = "password1"
            };
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(request);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(loginResponse.IsErrorResponse);
            Assert.AreEqual("User not found.", loginResponse.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.Login(LoginRequest)"/> sends a login error response is sent when the request has a username that is found but the password is incorrect.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestLoginFailsWithIncorrectPassword()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            LoginRequest request = new LoginRequest
            {
                UserName = TestUserName,
                Password = "password1"
            };
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(request);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(OkObjectResult));
            Assert.IsTrue(loginResponse.IsErrorResponse);
            Assert.AreEqual("Invalid password.", loginResponse.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }

        /// <summary>
        /// Tests the nominal case of the <see cref="UserController.Login(LoginRequest)"/> sends a successful response.
        /// Note: It is expected that none of the values retrieved will be null and warnings can be safely ignored.
        /// </summary>
        [TestMethod]
        public void TestSuccessfulLogin()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            passwordUtilityMock.Setup(utility => utility.VerifyPassword(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>())).Returns(true);
            LoginRequest request = new LoginRequest
            {
                UserName = TestUserName,
                Password = TestUserPassword,
            };
            Task<ActionResult<LoginResponse>> responseTask = Controller.Login(request);
            LoginResponse loginResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(OkObjectResult));
            Assert.IsFalse(loginResponse.IsErrorResponse);
            Assert.AreEqual(null, loginResponse.ErrorMessage);
            Assert.AreEqual(TestUserName, loginResponse.UserName);
            Assert.AreEqual(TestUserEmail, loginResponse.Email);
            Assert.IsTrue(loginResponse.UserId > 0);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the request is null.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithNullAddUserRequest()
        {
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(null);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("Request was null. Cannot add user.", addUserResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the full name is empty.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithEmptyUserFullName()
        {
            AddUserRequest request = new AddUserRequest
            {
                Name = string.Empty,
            };
            Task <ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("Name is empty. Cannot add user.", addUserResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the username is empty.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithEmptyUserName()
        {
            AddUserRequest request = new AddUserRequest
            {
                Name = "user",
                UserName = string.Empty,
            };
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("Username is empty. Cannot add user.", addUserResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the password is empty.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithEmptyPassword()
        {
            AddUserRequest request = new AddUserRequest
            {
                Name = "name",
                UserName = "username",
                Password = string.Empty
            };
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(BadRequestObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("Password is empty. Cannot add user.", addUserResponse.ErrorMessage);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the user already exists.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithExistingUser()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            AddUserRequest request = new AddUserRequest
            {
                Name = TestUserFullName,
                UserName = TestUserName,
                Password = TestUserPassword,
            };
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(OkObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("Username already exists. Cannot add user.", addUserResponse.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends a login error response is sent when the database fails to add user.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsWithInvalidValues()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            passwordUtilityMock.Setup(utility => utility.HashPassword(It.IsAny<string>())).Returns((null, null));
            AddUserRequest request = new AddUserRequest
            {
                Name = "name",
                UserName = "username",
                Password = "password"
            };
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(OkObjectResult));
            Assert.IsTrue(addUserResponse.IsErrorResponse);
            Assert.AreEqual("User could not be added to the system. Check server logs for more information.", addUserResponse.ErrorMessage);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }

        /// <summary>
        /// Tests that the <see cref="UserController.AddUserAccount(AddUserRequest)"/> sends successful response when all request parameters are valid.
        /// </summary>
        [TestMethod]
        public void TestAddUserFailsNominal()
        {
            dbContext = TestDbContextFactory.CreateApplicationDbContext();
            TestDbContextFactory.SeedDatabase(dbContext, TestUserFullName, TestUserName, TestUserPassword, TestUserEmail);
            Controller = new UserController(dbContext, loggerMock.Object, passwordUtilityMock.Object);
            AddUserRequest request = new AddUserRequest
            {
                Name = "name",
                UserName = "username",
                Password = "password",
                Email = "email"
            };
            passwordUtilityMock.Setup(utility => utility.HashPassword(It.IsAny<string>())).Returns((request.Password, [1, 2, 3]));
            Task<ActionResult<AddUserResponse>> responseTask = Controller.AddUserAccount(request);
            AddUserResponse addUserResponse = ExtractActionResultInnerResponseFromTask(responseTask, typeof(OkObjectResult));
            Assert.IsFalse(addUserResponse.IsErrorResponse);
            Assert.AreEqual(null, addUserResponse.ErrorMessage);
            Assert.AreEqual(request.UserName, addUserResponse.UserName);
            Assert.IsNotNull(addUserResponse.Email);
            Assert.IsTrue(addUserResponse.UserId > 0);
            TestDbContextFactory.DestroyDatabase(dbContext);
        }
    }
}
