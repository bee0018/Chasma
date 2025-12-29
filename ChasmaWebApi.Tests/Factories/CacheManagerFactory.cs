using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Objects;
using Moq;
using System.Collections.Concurrent;

namespace ChasmaWebApi.Tests.Factories
{
    /// <summary>
    /// Utility class for creating test cache object instances.
    /// </summary>
    public static class CacheManagerFactory
    {
        /// <summary>
        /// Seeds the cache manager with test data.
        /// </summary>
        /// <param name="cacheManagerMock">The mocked cache manager.</param>
        /// <param name="repoName">The name of the repository.</param>
        /// <param name="username">The username of the system.</param>
        public static void SeedCacheManager(Mock<ICacheManager> cacheManagerMock, string repoName, string username)
        {
            LocalGitRepository repo1 = CreateLocalGitRepository(1, repoName, username);
            LocalGitRepository repo2 = CreateLocalGitRepository(1, "Chasma", "batman");
            LocalGitRepository repo3 = CreateLocalGitRepository(123, "KirbyGray", "batman");
            ConcurrentDictionary<string, LocalGitRepository> repositories = new();
            repositories[repo1.Id] = repo1;
            repositories[repo2.Id] = repo2;
            repositories[repo3.Id] = repo3;

            ConcurrentDictionary<string, string> workingDirectories = new();
            workingDirectories[repo1.Id] = "path1";
            workingDirectories[repo2.Id] = "path2";
            workingDirectories[repo3.Id] = "path2";

            cacheManagerMock.SetupGet(mock => mock.Repositories).Returns(repositories);
            cacheManagerMock.SetupGet(mock => mock.WorkingDirectories).Returns(workingDirectories);
        }

        /// <summary>
        /// Creates a sample local git repository object.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="repoName">The repository name.</param>
        /// <param name="owner">The repository owner.</param>
        /// <returns>A sample local git repository instance.</returns>
        public static LocalGitRepository CreateLocalGitRepository(int userId, string repoName, string owner)
        {
            return new LocalGitRepository
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = repoName,
                Owner = owner,
                Url = "url",
            };
        }
    }
}
