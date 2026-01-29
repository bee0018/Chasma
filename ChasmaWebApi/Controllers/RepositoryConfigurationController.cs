using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
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
            .Where(i => i.UserId == userId && !i.IsIgnored)
            .ToList();
        LocalRepositoriesInfoMessage message = new()
        {
            Timestamp = DateTimeOffset.Now.ToString("g"),
            Repositories = repositories,
        };
        return Ok(message);
    }

    /// <summary>
    /// Gets the list of ignored repositories for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The message containing the list of ignored repository data.</returns>
    [HttpPost]
    [Route("retrieveIgnoredRepositories")]
    public ActionResult<GetIgnoredRepositoriesMessage> GetIgnoredRepositories(int userId)
    {
        logger.LogInformation("Getting ignored repositories for user with id: {userId}.", userId);
        List<string> repositories = cacheManager.Repositories.Values
            .Where(i => i.UserId == userId && i.IsIgnored)
            .Select(i => $"{i.Name}:{i.Id}")
            .ToList();
        GetIgnoredRepositoriesMessage message = new()
        {
            IgnoredRepositories = repositories,
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
    /// Adds a git repository from the specified path to cache.
    /// </summary>
    /// <param name="request">The request to add a local git repository.</param>
    /// <returns>The response to adding a git repository to the system.</returns>
    [HttpPost]
    [Route("addGitRepository")]
    public async Task<ActionResult<AddGitRepositoryResponse>> AddGitRepository([FromBody] AddGitRepositoryRequest request)
    {
        logger.LogInformation("Received an {request}.", nameof(AddGitRepositoryRequest));
        AddGitRepositoryResponse response = new();
        if (request == null)
        {
            logger.LogError("Received a null {request}. Sending error response.", nameof(AddGitRepositoryRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Request must be populated.";
            return BadRequest(response);
        }

        string repoPath = request.RepositoryPath;
        if (string.IsNullOrEmpty(repoPath))
        {
            logger.LogError("Invalid {request}. Repository path is required. Sending error response.", nameof(AddGitRepositoryRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Repository path is required.";
            return BadRequest(response);
        }

        int userId = request.UserId;
        if (!cacheManager.Users.TryGetValue(userId, out _))
        {
            logger.LogError("Invalid {request}. User with identifier {id} does not exist. Sending error response.", nameof(AddGitRepositoryRequest), userId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Could not find user.";
            return BadRequest(response);
        }

        if (!configurationManager.TryAddGitRepository(repoPath, userId, out LocalGitRepository localGitRepository, out string errorMessage))
        {
            logger.LogError("Failed to add git repository from path {repoPath}. Reason: {reason}", repoPath, errorMessage);
            response.IsErrorResponse = true;
            response.ErrorMessage = errorMessage;
            return Ok(response);
        }

        RepositoryModel repositoryModel = new()
        {
            Id = localGitRepository.Id,
            UserId = localGitRepository.UserId,
            Name = localGitRepository.Name,
            Owner = localGitRepository.Owner,
            Url = localGitRepository.Url,
            IsIgnored = false,
        };
        await applicationDbContext.Repositories.AddAsync(repositoryModel);
        WorkingDirectoryModel workingDirectoryModel = new()
        {
            RepositoryId = localGitRepository.Id,
            WorkingDirectory = repoPath,
        };
        await applicationDbContext.WorkingDirectories.AddAsync(workingDirectoryModel);
        await applicationDbContext.SaveChangesAsync();
        response.Repository = localGitRepository;
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
    /// Deletes the branch from the specified repository.
    /// </summary>
    /// <param name="request">The request to delete a branch.</param>
    /// <returns>Response to deleting a branch.</returns>
    [HttpDelete]
    [Route("deleteBranch")]
    public ActionResult<DeleteBranchResponse> DeleteBranch([FromBody] DeleteBranchRequest request)
    {
        DeleteBranchResponse response = new();
        if (request == null)
        {
            logger.LogError("Received a null {request}. Sending error response.", nameof(DeleteBranchRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Null request received. Request must be populated.";
            return BadRequest(response);
        }

        string repoId = request.RepositoryId;
        if (string.IsNullOrEmpty(repoId))
        {
            logger.LogError("Invalid request. Repository identifier is required. Sending error response.");
            response.IsErrorResponse = true;
            response.ErrorMessage = "Empty repository identifier received. Field must be populated.";
            return BadRequest(response);
        }

        string branchName = request.BranchName;
        if (string.IsNullOrEmpty(branchName))
        {
            logger.LogError("Invalid request. Branch name is required. Sending error response.");
            response.IsErrorResponse = true;
            response.ErrorMessage = "Empty branch name received. Field must be populated.";
            return BadRequest(response);
        }

        if (!configurationManager.TryDeleteBranch(repoId, branchName, out string errorMessage))
        {
            logger.LogError("Failure to delete branch: {reason}", errorMessage);
            response.IsErrorResponse = true;
            response.ErrorMessage = errorMessage;
            return Ok(response);
        }

        logger.LogInformation("Successfully deleted the branch {branchName}.", branchName);
        return Ok(response);
    }

    /// <summary>
    /// Applies the ignoring action on the specified repository.
    /// Note: this route will handle both cases of ignoring/including a repo based on the .
    /// </summary>
    /// <param name="request">The ignore repository request.</param>
    /// <returns>The ignore repository response.</returns>
    [HttpPost]
    [Route("ignoreRepository")]
    public async Task<ActionResult<IgnoreRepositoryResponse>> IgnoreRepository([FromBody] IgnoreRepositoryRequest request)
    {
        logger.LogInformation("Received a {request}", nameof(IgnoreRepositoryRequest));
        IgnoreRepositoryResponse response = new();
        if (request == null)
        {
            logger.LogError("Unable to ignore repository because {request} was null. Sending error response.", nameof(IgnoreRepositoryRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Request must not be empty.";
            return BadRequest(response);
        }

        string repoId = request.RepositoryId;
        if (string.IsNullOrEmpty(repoId))
        {
            logger.LogError("Unable to ignore repository because {request} was had an empty repository identifier. Sending error response.", nameof(IgnoreRepositoryRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Repository identifier is required.";
            return BadRequest(response);
        }

        if (!cacheManager.WorkingDirectories.TryGetValue(repoId, out _))
        {
            logger.LogError("No working directory was found for repository {repoId}, so it cannot be ignored. Sending error response.", repoId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "No working directory was found for the specified repository.";
            return BadRequest(response);
        }

        if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository localGitRepository))
        {
            logger.LogError("No repository was found with an identifier: {repoId}, so it cannot be ignored. Sending error response.", repoId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "No repository was found in cache.";
            return BadRequest(response);
        }

        int userId = request.UserId;
        RepositoryModel repositoryModel = await applicationDbContext.Repositories.FirstOrDefaultAsync(i => i.Id == repoId && i.UserId == userId);
        if (repositoryModel == null)
        {
            logger.LogError("Cannot ignore repository {repoId} because it does not exist in the database for user {userId}. Sending error response.", repoId, userId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "Cannot ignore repository because it does not exist in the database.";
            return Ok(response);
        }

        string repoName = localGitRepository.Name;
        bool isIgnored = request.IsIgnored;
        repositoryModel.IsIgnored = isIgnored;
        int rowsAffected = await applicationDbContext.SaveChangesAsync();
        if (rowsAffected <= 0)
        {
            logger.LogError("Database failed to update repository {repoName}. Sending error response", repoName);
            response.IsErrorResponse = true;
            response.ErrorMessage = "Failed to save changes to database. Check server logs for more information.";
            return Ok(response);
        }

        localGitRepository.IsIgnored = isIgnored;
        string action = isIgnored ? "ignored" : "included";
        logger.LogInformation("Successfully {action} repository {repoName} in cache and the database.", action, repoName);
        response.IncludedRepositories = cacheManager.Repositories.Values
            .Where(i => i.UserId == request.UserId && !i.IsIgnored)
            .ToList();
        return Ok(response);
    }
}