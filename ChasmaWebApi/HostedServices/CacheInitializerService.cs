using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.HostedServices
{
    /// <summary>
    /// Class representing a hosted service that initializes the cache with database information upon application startup.
    /// </summary>
    /// <param name="logger">The internal logging interface.</param>
    /// <param name="cacheManager">The application's cache manager.</param>
    /// <param name="serviceScopeFactory">The service scope factory used for getting required services.</param>
    public class CacheInitializerService(ILogger<CacheInitializerService> logger, ICacheManager cacheManager, IServiceScopeFactory serviceScopeFactory) : IHostedService
    {
        /// <summary>
        /// The internal logging interface.
        /// </summary>
        private readonly ILogger<CacheInitializerService> logger = logger;

        /// <summary>
        /// The application's cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager = cacheManager;

        /// <summary>
        /// The service scope factory used for getting required services.
        /// </summary>
        private readonly IServiceScopeFactory serviceScopeFactory = serviceScopeFactory;

        // <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => InitializeCacheAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        // <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping the CacheInitializer hosted service and clearing cache.");
            cacheManager.WorkingDirectories.Clear();
            cacheManager.Repositories.Clear();
            cacheManager.Users.Clear();
            logger.LogInformation("Cache cleared successfully.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the cache with data stored in the database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        private async Task InitializeCacheAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Initializing the cache with the database information.");
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            ApplicationDbContext applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await applicationDbContext.Database.MigrateAsync(cancellationToken);
            List<RepositoryModel> repositories = await applicationDbContext.Repositories.ToListAsync(cancellationToken);
            foreach (RepositoryModel repoModel in repositories)
            {
                LocalGitRepository repository = new LocalGitRepository()
                {
                    Id = repoModel.Id,
                    UserId = repoModel.UserId,
                    Name = repoModel.Name,
                    Owner = repoModel.Owner,
                    Url = repoModel.Url,
                    IsIgnored = repoModel.IsIgnored,
                };
                cacheManager.Repositories.TryAdd(repository.Id, repository);
            }

            List<WorkingDirectoryModel> workingDirectories = await applicationDbContext.WorkingDirectories.ToListAsync(cancellationToken);
            foreach (WorkingDirectoryModel workingDirectoryModel in workingDirectories)
            {
                cacheManager.WorkingDirectories.TryAdd(workingDirectoryModel.RepositoryId, workingDirectoryModel.WorkingDirectory);
            }

            List<UserAccountModel> users = await applicationDbContext.UserAccounts.ToListAsync(cancellationToken);
            foreach (UserAccountModel user in users)
            {
                cacheManager.Users.TryAdd(user.Id, user);
            }

            logger.LogInformation("Finished updating the cache with the database data.");
        }
    }
}
