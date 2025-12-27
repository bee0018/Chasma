using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Requests;
using ChasmaWebApi.Data.Responses;
using ChasmaWebApi.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the user controller for database CRUD operations.
    /// </summary>
    [ApiController]
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
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        /// <param name="log">The injected internal logger.</param>
        public UserController(ApplicationDbContext dbContext, ILogger<UserController> log)
        {
            applicationDbContext = dbContext;
            logger = log;
        }

        /// <summary>
        /// Logs in the specified user to the system.
        /// </summary>
        /// <param name="request">The request to log the user in.</param>
        /// <returns>A login response.</returns>
        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            LoginResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request was null. Cannot login user.";
                logger.LogError("AddUserRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Username is empty. Cannot login user.";
                logger.LogError("Username is empty. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Password is empty. Cannot login user.";
                logger.LogError("Password is empty. Sending error response");
                return BadRequest(response);
            }

            UserAccountModel? account = await applicationDbContext.UserAccounts.FirstOrDefaultAsync(u => u.UserName == request.UserName);
            if (account == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "User not found.";
                logger.LogError("User {username} not found. Sending error response", request.UserName);
                return BadRequest(response);
            }

            bool isPasswordValid = PasswordUtility.VerifyPassword(request.Password, account.Salt, account.Password);
            if (!isPasswordValid)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Invalid password.";
                logger.LogError("Invalid password for user {username}. Sending error response", request.UserName);
                return Ok(response);
            }

            logger.LogInformation("User {username} logged in successfully", account.UserName);
            response.UserName = account.UserName;
            response.UserId = account.Id;
            response.Email = account.Email;
            return Ok(response);
        }

        /// <summary>
        /// Adds a user account to the database.
        /// </summary>
        /// <param name="request">The request containg the account to be added to the database.</param>
        /// <returns>Result signifying if the operation was successful or not.</returns>
        [HttpPost]
        [Route("addUserAccount")]
        public async Task<ActionResult<AddUserResponse>> AddUserAccount([FromBody] AddUserRequest request)
        {
            AddUserResponse response = new();
            if (request == null)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request was null. Cannot add user.";
                logger.LogError("AddUserRequest received is null. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Name))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Name is empty. Cannot add user.";
                logger.LogError("Name is empty. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Username is empty. Cannot add user.";
                logger.LogError("Username is empty. Sending error response");
                return BadRequest(response);
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Password is empty. Cannot add user.";
                logger.LogError("Password is empty. Sending error response");
                return BadRequest(response);
            }

            if (await applicationDbContext.UserAccounts.AnyAsync(u => u.UserName == request.UserName))
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "Username already exists. Cannot add user.";
                logger.LogError("Username {username} already exists. Sending error response", request.UserName);
                return Ok(response);
            }

            (string hashedPassword, byte[] salt) = PasswordUtility.HashPassword(request.Password);
            UserAccountModel account = new()
            {
                Name = request.Name,
                UserName = request.UserName,
                Email = request.Email,
                Password = hashedPassword,
                Salt = salt,
            };
            await applicationDbContext.UserAccounts.AddAsync(account);
            int rowsAffected = await applicationDbContext.SaveChangesAsync();
            if (rowsAffected <= 0)
            {
                response.IsErrorResponse = true;
                response.ErrorMessage = "User could not be added to the system. Check server logs for more information.";
                logger.LogError("User {username} could not be added to the system. Sending error response", account.UserName);
                return Ok(response);
            }

            logger.LogInformation("User {username} has been added to the system successfully", account.UserName);
            response.UserName = account.UserName;
            response.UserId = account.Id;
            response.Email = account.Email;
            return Ok(response);
        }
    }
}
