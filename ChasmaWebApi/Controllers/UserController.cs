using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Requests.Status;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Data.Responses.Status;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the user controller for database CRUD operations.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;
        
        /// <summary>
        /// The internal logger for logging status.
        /// </summary>
        private readonly ILogger<UserController> logger;

        /// <summary>
        /// The internal password utility for hashing and verifying passwords.
        /// </summary>
        private readonly IPasswordUtility passwordUtility;

        /// <summary>
        /// The internal cache manager.
        /// </summary>
        private readonly ICacheManager cacheManager;

        /// <summary>
        /// The internal web API configurations.
        /// </summary>
        private readonly ChasmaWebApiConfigurations apiConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        /// <param name="log">The injected internal logger.</param>
        /// <param name="passwordUtil">The injected password utility.</param>
        /// <param name="apiCacheManager">The internal API cache manager.</param>
        /// <param name="config">The internal web API configurations.</param>
        public UserController(ApplicationDbContext dbContext, ILogger<UserController> log, IPasswordUtility passwordUtil, ICacheManager apiCacheManager, ChasmaWebApiConfigurations config)
        {
            applicationDbContext = dbContext;
            logger = log;
            passwordUtility = passwordUtil;
            cacheManager = apiCacheManager;
            apiConfiguration = config;
        }

        /// <summary>
        /// Logs in the specified user to the system.
        /// </summary>
        /// <param name="request">The request to log the user in.</param>
        /// <returns>A login response.</returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            LoginResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Failed login attempt at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            if (string.IsNullOrEmpty(apiConfiguration.JwtSecretKey) || apiConfiguration.JwtSecretKey.Length < 16)
            {
                logger.LogWarning("Login attempt blocked - system not configured.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "System is not configured. Please complete setup first.";
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Username is empty. Cannot login user.";
                logger.LogError("Failed login attempt at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Failed login attempt at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            UserAccountModel? account = await applicationDbContext.UserAccounts.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (account == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Failed login attempt at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            bool isPasswordValid = passwordUtility.VerifyPassword(request.Password, account.Salt, account.Password);
            if (!isPasswordValid)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Failed login attempt at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            account.RefreshToken = GenerateRefreshToken();
            account.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
            await applicationDbContext.SaveChangesAsync();

            logger.LogInformation("User {username} logged in successfully", account.UserName);
            ApplicationUserPermissions permissions = new()
            {
                IsUsingGitHubApi = !string.IsNullOrEmpty(apiConfiguration.GitHubApiToken),
                IsUsingGitLabApi = !string.IsNullOrEmpty(apiConfiguration.GitLabApiToken),
                IsUsingBitbucketApi = !string.IsNullOrEmpty(apiConfiguration.BitbucketApiToken),
            };
            ApplicationUser user = new()
            {
                UserId = account.Id,
                UserName = account.UserName,
                Email = account.Email,
                Permissions = permissions,
            };
            response.User = user;
            response.Token = GenerateAccessToken(account);
            response.RefreshToken = account.RefreshToken;
            return Ok(response);
        }

        /// <summary>
        /// Adds a user account to the database.
        /// </summary>
        /// <param name="request">The request containg the account to be added to the database.</param>
        /// <returns>Result signifying if the operation was successful or not.</returns>
        [AllowAnonymous]
        [HttpPost]
        [Route("addUserAccount")]
        public async Task<ActionResult<AddUserResponse>> AddUserAccount([FromBody] AddUserRequest request)
        {
            AddUserResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("AddUserRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(apiConfiguration.JwtSecretKey) || apiConfiguration.JwtSecretKey.Length < 16)
            {
                logger.LogWarning("Login attempt blocked - system not configured.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "System is not configured. Please complete setup first.";
                return Ok(response);
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Name is empty. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Username is empty. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Password is empty. Sending error response");
                return BadRequest(response);
            }

            if (await applicationDbContext.UserAccounts.AnyAsync(u => u.UserName == request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid username or password.";
                logger.LogError("Username {username} already exists. Sending error response", request.UserName);
                return Ok(response);
            }

            (string hashedPassword, byte[] salt) = passwordUtility.HashPassword(request.Password);
            UserAccountModel account = new()
            {
                Name = request.Name,
                UserName = request.UserName,
                Email = request.Email,
                Password = hashedPassword,
                Salt = salt,
                RefreshToken = GenerateRefreshToken(),
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(7),
            };
            try
            {
                await applicationDbContext.UserAccounts.AddAsync(account);
                int rowsAffected = await applicationDbContext.SaveChangesAsync();
                logger.LogInformation("User {username} has been added to the system successfully", account.UserName);
                cacheManager.Users.TryAdd(account.Id, account);
                ApplicationUserPermissions permissions = new()
                {
                    IsUsingGitHubApi = !string.IsNullOrEmpty(apiConfiguration.GitHubApiToken),
                    IsUsingGitLabApi = !string.IsNullOrEmpty(apiConfiguration.GitLabApiToken),
                    IsUsingBitbucketApi = !string.IsNullOrEmpty(apiConfiguration.BitbucketApiToken),
                };
                ApplicationUser user = new()
                {
                    UserId = account.Id,
                    UserName = account.UserName,
                    Email = account.Email,
                    Permissions = permissions,
                };
                response.User = user;
                response.Token = GenerateAccessToken(account);
                response.RefreshToken = account.RefreshToken;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "User could not be added to the system. Check server logs for more information.";
                logger.LogError(ex, "User {username} could not be added to the system. Sending error response", account.UserName);
                return Ok(response);
            }
        }

        /// <summary>
        /// Refreshes the access token for the user based on the provided refresh token.
        /// </summary>
        /// <param name="request">The refresh token request.</param>
        /// <returns>The refresh token response.</returns>
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshRequest request)
        {
            RefreshResponse response = new();
            if (request == null)
            {
                logger.LogError("Null refresh request recieved. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid token received.";
                return Unauthorized(response);
            }

            string refreshToken = request.RefreshToken;
            if (string.IsNullOrEmpty(refreshToken))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid token recieved.";
                logger.LogError("Invalid refresh token recieved at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            UserAccountModel user = await applicationDbContext.UserAccounts.FirstOrDefaultAsync(i => i.RefreshToken == refreshToken);
            if (user == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid token received.";
                logger.LogError("No user found when trying to validate refresh token recieved at {now}. Sending error response.", DateTimeOffset.Now);
                return Unauthorized(response);
            }

            if (user.RefreshTokenExpiration < DateTime.UtcNow)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Session has expired. Login again.";
                logger.LogError("Session has expired at {time}. Sending error response.", user.RefreshTokenExpiration);
                return Unauthorized(response);
            }

            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiration = DateTime.UtcNow.AddDays(7);
            await applicationDbContext.SaveChangesAsync();

            response.Token = GenerateAccessToken(user);
            response.RefreshToken = user.RefreshToken;
            return Ok(response);
        }

        /// <summary>
        /// Logs out the specified user by invalidating their refresh token.
        /// </summary>
        /// <param name="request">The logout request containing the user identifier. Cannot be null.</param>
        /// <returns>Response that indicates whether the logout was successful.</returns>
        [HttpPost]
        [Route("logout")]
        public async Task<ActionResult<LogoutResponse>> Logout([FromBody] LogoutRequest request)
        {
            LogoutResponse response = new();
            if (request == null)
            {
                logger.LogError("Null logout request recieved. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid logout request received.";
                return Unauthorized(response);
            }

            int userId = request.UserId;
            UserAccountModel? user = applicationDbContext.UserAccounts.FirstOrDefault(i => i.Id == userId);
            if (user == null)
            {
                logger.LogError("Failed to logout user. Sending error response.");
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid logout request received.";
                return Unauthorized(response);
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiration = DateTime.UtcNow;
            await applicationDbContext.SaveChangesAsync();
            logger.LogInformation("User {username} logged out successfully. Tokens invalidated.", user.UserName);
            return Ok(response);
        }

        /// <summary>
        /// Generates the access token for the user.
        /// </summary>
        /// <param name="account">The user to provide the access token for.</param>
        /// <returns>The generate token.</returns>
        private string GenerateAccessToken(UserAccountModel account)
        {
            List<Claim> claims =
                    [
                        new(ClaimTypes.Name, account.UserName),
                        new(ClaimTypes.NameIdentifier, account.Id.ToString())
                    ];
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(apiConfiguration.JwtSecretKey));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken token = new(
                issuer: "ChasmaWebApi",
                audience: "ChasmaThinClient",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a new random refresh token.
        /// </summary>
        /// <returns>The refresh token.</returns>
        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
