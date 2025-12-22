using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Class containing the repository configuration CRUD routes.
/// </summary>
[Route("api/[controller]")]
public class RepositoryConfigurationController : ControllerBase
{
    /// <summary>
    /// The internal API logger.
    /// </summary>
    private readonly ILogger<RepositoryConfigurationController> logger;

    /// <summary>
    /// The repository configuration manager.
    /// </summary>
    private readonly IRepositoryConfigurationManager configurationManager;

    /// <summary>
    /// The internal cache manager.
    /// </summary>
    private readonly ICacheManager cacheManager;

    /// <summary>
    /// The database context used for interacting with the database.
    /// </summary>
    private readonly ApplicationDbContext applicationDbContext;

    #region Constructor

    /// <summary>
    /// Instantiates a new <see cref="RepositoryConfigurationController"/> class.
    /// </summary>
    /// <param name="log">The internal API logger.</param>
    /// <param name="configManager">The repository configuration manager.</param>
    /// <param name="internalCacheManager">The internal cache manager.</param>
    /// <param name="dbContext">The application database context.</param>
    public RepositoryConfigurationController(ILogger<RepositoryConfigurationController> log, IRepositoryConfigurationManager configManager, ICacheManager internalCacheManager, ApplicationDbContext dbContext)
    {
        logger = log;
        configurationManager = configManager;
        cacheManager = internalCacheManager;
        applicationDbContext = dbContext;
        PopulateCache();
    }

    #endregion

    /// <summary>
    /// Gets the local git repositories associated with the specified user from cache.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>Message containing the current repositories the user owns.</returns>
    [HttpGet]
    [Route("getLocalGitRepositories")]
    public ActionResult<LocalRepositoriesInfoMessage> GetLocalGitRepositories(int userId)
    {
        logger.LogInformation("Getting repositories for user with id: {userId}.", userId);
        List<LocalGitRepository> repositories = cacheManager.Repositories.Values
            .Where(i => i.UserId == userId)
            .ToList();
        LocalRepositoriesInfoMessage message = new()
        {
            Timestamp = DateTimeOffset.Now.ToString("g"),
            Repositories = repositories,
        };
        return Ok(message);
    }

    /// <summary>
    /// Gets the valid git repositories found on this system.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>Message containing the git repositories found on the logical drives.</returns>
    [HttpPost]
    [Route("addLocalGitRepositories")]
    public async Task<ActionResult<AddLocalRepositoriesResponse>> AddLocalGitRepositories(int userId)
    {
        logger.LogInformation("Attempting to add local git repositories on the machine's logical drives.");
        AddLocalRepositoriesResponse response = new();
        if (!configurationManager.TryAddLocalGitRepositories(userId, out List<LocalGitRepository> newRepositories))
        {
            logger.LogError("No new local git repositories added.");
            response.IsErrorResponse = true;
            response.ErrorMessage = "No new local git repositories found on this machine.";
            return Ok(response);
        }
        
        logger.LogInformation("Found {count} new repo(s) on this machine.", newRepositories.Count);
        foreach (LocalGitRepository repo in newRepositories)
        {
            RepositoryModel repositoryModel = new()
            {
                Id = repo.Id,
                UserId = repo.UserId,
                Name = repo.Name,
                Owner = repo.Owner,
                Url = repo.Url,
            };
            await applicationDbContext.Repositories.AddAsync(repositoryModel);

            WorkingDirectoryModel workingDirectoryModel = new()
            {

                RepositoryId = repo.Id,
                WorkingDirectory = cacheManager.WorkingDirectories[repo.Id],
            };
            await applicationDbContext.WorkingDirectories.AddAsync(workingDirectoryModel);
        }

        await applicationDbContext.SaveChangesAsync();
        response.CurrentRepositories = cacheManager.Repositories.Values
            .Where(i => i.UserId == userId)
            .ToList();
        return Ok(response);
    }

    /// <summary>
    /// Deletes the git repositories from with the specified repository key from cache.
    /// </summary>
    /// <param name="request">The delete repository request.</param>
    /// <returns>A delete repository response.</returns>
    [HttpDelete]
    [Route("deleteRepository")]
    public async Task<ActionResult<DeleteRepositoryResponse>> DeleteRepository([FromBody] DeleteRepositoryRequest request)
    {
        DeleteRepositoryResponse response = new();
        if (request == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Null request was received. Cannot delete repo.";
            logger.LogError("Invalid request received to delete repository from cache.");
            return BadRequest(response);
        }

        if (string.IsNullOrEmpty(request.RepositoryId))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Repository identifier is required.";
            logger.LogError("Invalid request received to delete repository from cache.");
            return BadRequest(response);
        }

        logger.LogInformation("Attempting to delete repository with key {repoKey} from cache.", request.RepositoryId);
        if (!configurationManager.TryDeleteRepository(request.RepositoryId, request.UserId, out List<LocalGitRepository> localGitRepositories, out string errorMessage))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = errorMessage;
            logger.LogError("Failed to delete repository with key {repoKey} from cache. Error: {errorMessage}", request.RepositoryId, errorMessage);
            return Ok(response);
        }

        List<RepositoryModel> reposToDelete = applicationDbContext.Repositories
            .Where(i => i.UserId == request.UserId && i.Id == request.RepositoryId)
            .ToList();
        foreach (RepositoryModel repo in reposToDelete)
        {
            applicationDbContext.Repositories.Remove(repo);
            WorkingDirectoryModel directory = await applicationDbContext.WorkingDirectories.FirstAsync(i => i.RepositoryId == repo.Id);
            applicationDbContext.WorkingDirectories.Remove(directory);
        }

        await applicationDbContext.SaveChangesAsync();
        response.Repositories = localGitRepositories;
        return Ok(response);
    }

    /// <summary>
    /// Populates the cache from the database on controller instantiation.
    /// </summary>
    private void PopulateCache()
    {
        foreach (RepositoryModel repo in applicationDbContext.Repositories)
        {
            LocalGitRepository localRepo = new()
            {
                Id = repo.Id,
                UserId = repo.UserId,
                Name = repo.Name,
                Owner = repo.Owner,
                Url = repo.Url,
            };
            cacheManager.Repositories.TryAdd(localRepo.Id, localRepo);
        }

        foreach (WorkingDirectoryModel workingDirectoryModel in applicationDbContext.WorkingDirectories)
        {
            cacheManager.WorkingDirectories.TryAdd(workingDirectoryModel.RepositoryId, workingDirectoryModel.WorkingDirectory);
        }

        foreach (UserAccountModel user in applicationDbContext.UserAccounts)
        {
            cacheManager.Users.TryAdd(user.Id, user);
        }
    }
}