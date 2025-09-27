using ChasmaWebApi.Controllers;
using ChasmaWebApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChasmaWebApi.Tests
{
    /// <summary>
    /// Class testing the functionality of data CRUD operations in the Chasma Web API.
    /// </summary>
    [TestClass]
    public class DatabaseConfigurationTests
    {
        /// <summary>
        /// The service provider used for adding and providing external services.
        /// </summary>
        #pragma warning disable CS8618 
        private ServiceProvider serviceProvider;

        /// <summary>
        /// Sets up the resources for each unit test to use.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            ApplicationDbContext context = new(options);

            serviceProvider = new ServiceCollection()
                .AddSingleton(context)
                .AddSingleton<DatabaseController>()
                .BuildServiceProvider();
        }

        /// <summary>
        /// Disposes the resources after each test is concluded.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            serviceProvider.Dispose();
        }

        /// <summary>
        /// Tests that the <see cref="DatabaseController.AddUserAccount(UserAccount)"/> successfully adds a user in the nominal case.
        /// </summary>
        [TestMethod]
        public void TestNominalAddUserMethod()
        {
            DatabaseController databaseController = serviceProvider.GetRequiredService<DatabaseController>();
            UserAccount account = new() {
                Name = "test",
                UserName = "username",
                Password = "password"
            };
            ActionResult actionResult = databaseController.AddUserAccount(account).Result;
            Assert.IsInstanceOfType<OkObjectResult>(actionResult);
        }

        /// <summary>
        /// Tests that the <see cref="DatabaseController.AddUserAccount(UserAccount)"/> successfully handles case when
        /// user attempts to add an account with invalid data.
        /// </summary>
        [TestMethod]
        public void TestAddUserMethodWithInvalidAccount()
        {
            DatabaseController databaseController = serviceProvider.GetRequiredService<DatabaseController>();
            UserAccount account = new();
            Task<ActionResult> actionResult = databaseController.AddUserAccount(account);
            ObjectResult? objectResult = actionResult.Result as ObjectResult;
            Assert.IsNotNull(objectResult);

            ProblemDetails? problemDetails = objectResult.Value as ProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.IsNotNull(problemDetails.Detail);
            Assert.IsTrue(problemDetails.Detail.Equals("Error attempting to add user to the database."));
        }
    }
}
