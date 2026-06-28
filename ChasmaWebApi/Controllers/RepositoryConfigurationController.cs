using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Messages;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Git;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Class containing the repository configuration CRUD routes.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(ChasmaWebApiConfigurations.RateLimiterPolicy)]
public class RepositoryConfigurationController : ControllerBase
{
    /// <summary>
    /// The internal API logger.
    /// </summary>
    private readonly ILogger<RepositoryConfigurationController> logger;

    /// <summary>
    /// The internal API application control service for managing application-level operations.
    /// </summary>
    private readonly IApplicationControlService applicationControlService;

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
    /// <param name="controlService">The internal application control service.</param>
    /// <param name="internalCacheManager">The internal cache manager.</param>
    /// <param name="dbContext">The application database context.</param>
    public RepositoryConfigurationController(ILogger<RepositoryConfigurationController> log, IApplicationControlService controlService, ICacheManager internalCacheManager, ApplicationDbContext dbContext)
    {
        logger = log;
        applicationControlService = controlService;
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
            .OrderBy(i => i.GetDisplayName())
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
            .Select(i => $"{i.GetDisplayName()}:{i.Id}")
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
        if (!applicationControlService.TryAddLocalGitRepositoriesFromFileSystem(userId, out List<LocalGitRepository> newRepositories))
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
                HostPlatform = repo.HostPlatform,
                IsIgnored = false,
                DisplayName = repo.DisplayName,
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
    public async Task<ActionResult<AddGitRepositoriesResponse>> AddGitRepository([FromBody] AddGitRepositoriesRequest request)
    {
        logger.LogInformation("Received an {request}.", nameof(AddGitRepositoriesRequest));
        AddGitRepositoriesResponse response = new();
        if (request == null)
        {
            logger.LogError("Received a null {request}. Sending error response.", nameof(AddGitRepositoriesRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Request must be populated.";
            return BadRequest(response);
        }

        List<string> repoPaths = request.RepositoryPaths;
        if (repoPaths.Count == 0)
        {
            logger.LogError("Invalid {request}. One or more repository paths are required. Sending error response.", nameof(AddGitRepositoriesRequest));
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. One or more repository paths are required.";
            return BadRequest(response);
        }

        int userId = request.UserId;
        if (!cacheManager.Users.TryGetValue(userId, out _))
        {
            logger.LogError("Invalid {request}. User with identifier {id} does not exist. Sending error response.", nameof(AddGitRepositoriesRequest), userId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Could not find user.";
            return BadRequest(response);
        }

        List<RepositoryAdditionResult> additionResults = applicationControlService.AddGitRepositories(repoPaths, userId, out List<NewRepository> newRepositories);
        await AddRepositoriesToDatabase(newRepositories);
        response.Repositories = newRepositories.Select(i => i.Repository).ToList();
        response.AdditionResults = additionResults;
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
        if (!applicationControlService.TryDeleteRepository(request.RepositoryId, request.UserId, out List<LocalGitRepository> localGitRepositories, out string errorMessage))
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

        string repoName = localGitRepository.GetDisplayName();
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

    /// <summary>
    /// Deletes the specified file from the repository it belongs to.
    /// </summary>
    /// <param name="request">The request to remove a file.</param>
    /// <returns>The response to delete a file.</returns>
    [HttpDelete]
    [Route("removeFile")]
    public ActionResult<GitRmResponse> RemoveFile([FromBody] GitRmRequest request)
    {
        GitRmResponse response = new();
        string requestName = nameof(GitRmRequest);
        if (request == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Null request was received. Cannot remove file.";
            logger.LogError("Invalid {request} received to remove file from repository.", requestName);
            return BadRequest(response);
        }

        RepositoryStatusElement selectedFile = request.SelectedFile;
        if (selectedFile == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Selected file is required.";
            logger.LogError("Invalid {request} received to remove file from repository because the selected file was null.", requestName);
            return Ok(response);
        }

        logger.LogInformation("Attempting to remove file {fileName} from repository with key {repoKey}.", selectedFile.FilePath, selectedFile.RepositoryId);
        if (!applicationControlService.TryDeleteFile(selectedFile, out string errorMessage))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = errorMessage;
            logger.LogError("Failed to remove file {fileName} from repository with key {repoKey}. Error: {errorMessage}", selectedFile.FilePath, selectedFile.RepositoryId, errorMessage);
            return Ok(response);
        }

        logger.LogInformation("Successfully removed file {fileName} from repository with key {repoKey}.", selectedFile.FilePath, selectedFile.RepositoryId);
        return Ok(response);
    }

    /// <summary>
    /// Clones the list of git repositories from the specified clone information and adds them to cache.
    /// </summary>
    /// <param name="request">The request to add multiple repositories.</param>
    /// <returns>The response to cloning a repository.</returns>
    [HttpPost]
    [Route("gitClone")]
    public async Task<ActionResult<GitCloneResponse>> CloneRepositories([FromBody] GitCloneRequest request)
    {
        string requestName = nameof(GitCloneRequest);
        GitCloneResponse response = new();
        if (request == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = $"Null {requestName} was received. Cannot start cloning operation.";
            logger.LogError("Invalid {request} received to clone repositories.", requestName);
            return BadRequest(response);
        }

        List<GitCloneBlueprint> blueprints = request.Blueprints;
        if (blueprints.Count == 0)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Nothing to do because there are no repositories.";
            logger.LogError("Invalid {request} received to clone repositories because no blueprints were provided.", requestName);
            return Ok(response);
        }

        List<RepositoryAdditionResult> additionResults = applicationControlService.CloneGitRepositories(blueprints, request.UserId, out List<NewRepository> newRepositories);
        await AddRepositoriesToDatabase(newRepositories);
        logger.LogInformation("Finished cloning repositories. Successfully cloned {successCount} repo(s) and failed to clone {failureCount} repo(s).", newRepositories.Count, additionResults.Count - newRepositories.Count);
        response.Repositories = newRepositories.Select(i => i.Repository).ToList();
        response.AdditionResults = additionResults;
        return Ok(response);
    }

    /// <summary>
    /// Changes the repository display name for the specified repository.
    /// </summary>
    /// <param name="request">The request to change the repository display name.</param>
    /// <returns>The response to changing the repository display name.</returns>
    [HttpPost]
    [Route("changeRepositoryDisplayName")]
    public async Task<ActionResult<ChangeRepositoryDisplayNameResponse>> ChangeRepositoryDisplayName([FromBody] ChangeRepositoryDisplayNameRequest request)
    {
        ChangeRepositoryDisplayNameResponse response = new();
        string requestName = nameof(ChangeRepositoryDisplayNameRequest);
        if (request == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Null request was received. Cannot change repository display name.";
            logger.LogError("Invalid {request} received to change repository display name.", requestName);
            return BadRequest(response);
        }

        string repoId = request.RepositoryId;
        if (string.IsNullOrEmpty(repoId))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Repository identifier is required.";
            logger.LogError("Invalid {request} received to change repository display name because the repository identifier was null or empty.", requestName);
            return Ok(response);
        }

        if (!cacheManager.Repositories.TryGetValue(repoId, out LocalGitRepository repository))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "No repository was found in cache with the specified identifier.";
            logger.LogError("No repository was found in cache with an identifier: {repoId}. Cannot change display name. Sending error response.", repoId);
            return Ok(response);
        }

        string newDisplayName = request.NewName;
        if (string.IsNullOrEmpty(newDisplayName))
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. New display name is required.";
            logger.LogError("Invalid {request} received to change repository display name because the new display name was null or empty.", requestName);
            return Ok(response);
        }

        if (newDisplayName == repository.DisplayName)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "Invalid request. Display name must not be the current display name.";
            logger.LogError("Invalid {request} received to change repository display name because the new display is the same as the current display name. Sending error response.", requestName);
            return Ok(response);
        }

        RepositoryModel repositoryModel = await applicationDbContext.Repositories.FirstOrDefaultAsync(i => i.Id == repoId);
        if (repositoryModel == null)
        {
            response.IsErrorResponse = true;
            response.ErrorMessage = "No repository was found in the database with the specified identifier.";
            logger.LogError("No repository was found in the database with an identifier: {repoId}. Cannot change display name. Sending error response.", repoId);
            return Ok(response);
        }

        try
        {
            repositoryModel.DisplayName = newDisplayName;
            int rowsAffected = await applicationDbContext.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Failed to save changes to the database. Check server logs for more information.";
                logger.LogError("Database failed to update repository display name for repository with id {repoId}. Sending error response.", repoId);
                return Ok(response);
            }

            logger.LogInformation("Successfully changed repository display name to {newDisplayName} in cache and the database.", newDisplayName);
            repository.DisplayName = newDisplayName;
            response.Repository = repository;
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception was thrown while attempting to change the repository display name for repository with id {repoId}. Sending error response.", repoId);
            response.IsErrorResponse = true;
            response.ErrorMessage = "An exception was thrown while attempting to change the repository display name. Check server logs for more information.";
            return Ok(response);
        }
    }

    #region Private Methods
    
    /// <summary>
    /// Adds the specified repositories to the database.
    /// </summary>
    /// <param name="newRepositories">The new repositories to add to the database.</param>
    /// <returns>The completed task.</returns>
    private async Task AddRepositoriesToDatabase(IEnumerable<NewRepository> newRepositories)
    {
        foreach (NewRepository newRepository in newRepositories)
        {
            string repoPath = newRepository.WorkingDirectory;
            LocalGitRepository localGitRepository = newRepository.Repository;
            RepositoryModel repositoryModel = new()
            {
                Id = localGitRepository.Id,
                UserId = localGitRepository.UserId,
                Name = localGitRepository.Name,
                Owner = localGitRepository.Owner,
                Url = localGitRepository.Url,
                HostPlatform = localGitRepository.HostPlatform,
                IsIgnored = false,
                DisplayName = localGitRepository.DisplayName,
            };
            await applicationDbContext.Repositories.AddAsync(repositoryModel);
            WorkingDirectoryModel workingDirectoryModel = new()
            {
                RepositoryId = localGitRepository.Id,
                WorkingDirectory = repoPath,
            };
            await applicationDbContext.WorkingDirectories.AddAsync(workingDirectoryModel);
        }

        await applicationDbContext.SaveChangesAsync();
    }

    #endregion
}