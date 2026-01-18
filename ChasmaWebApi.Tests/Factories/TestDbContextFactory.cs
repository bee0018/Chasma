using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Tests.Factories
{
    /// <summary>
    /// Utility class for creating test database context instances.
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new application database context for testing.
        /// </summary>
        /// <returns>The test instance of the application database context.</returns>
        public static ApplicationDbContext CreateApplicationDbContext()
        {
            string guid = Guid.NewGuid().ToString();
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(guid).Options;
            ApplicationDbContext context = new ApplicationDbContext(options);
            return context;
        }

        /// <summary>
        /// Seeds the database with initial data for testing.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="fullName">The full name of the user.</param>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <param name="email">The email of the user.</param>
        public static void SeedDatabase(ApplicationDbContext context, string fullName, string username, string password, string email)
        {
            UserAccountModel testUser = new()
            {
                Name = fullName,
                UserName = username,
                Password = password,
                Email = email,
                Salt = [],
            };
            context.UserAccounts.Add(testUser);

            RepositoryModel ignoredRepo = new()
            {
                Id = "testRepo1234",
                Owner = "chasma",
                UserId = 100,
                IsIgnored = true,
                Name = "chasma1234",
                Url = "url.com"
            };
            RepositoryModel nominalRepo = new()
            {
                Id = "testRepo12345",
                Owner = "chasma",
                UserId = 100,
                IsIgnored = false,
                Name = "chasma12345",
                Url = "url.com"
            };
            context.Repositories.Add(ignoredRepo);
            context.Repositories.Add(nominalRepo);
            context.SaveChanges();
        }

        /// <summary>
        /// Destroys the test database and disposes of the context.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public static void DestroyDatabase(ApplicationDbContext context)
        {
            context.Database.EnsureDeleted();
            context.Dispose();
        }
    }
}
