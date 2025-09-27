using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Requests;
using ChasmaWebApi.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the controller for logging into the system.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Instantiates a new instance of the <see cref="LoginController"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        public LoginController(ApplicationDbContext dbContext)
        {
            applicationDbContext = dbContext;
        }

        /// <summary>
        /// Handles the <see cref="LoginRequest"/> sent by an external client.
        /// </summary>
        /// <param name="loginRequest">The login request information.</param>
        /// <returns>The login response.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<LoginResponse>> HandleLoginRequest(LoginRequest loginRequest)
        {
            if (loginRequest == null)
            {
                return BadRequest("Request body cannot be null");
            }

            if (string.IsNullOrEmpty(loginRequest.UserName))
            {
                return BadRequest("Username cannot be empty.");
            }

            if (string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest("Password cannot be empty.");
            }

            LoginResponse loginResponse = new()
            {
                IsErrorMessage = false,
                Message = string.Empty,
                Name = string.Empty,
                UserName = string.Empty,
            };

            UserAccount? user = await applicationDbContext.UserAccounts.FirstOrDefaultAsync(i => i.UserName == loginRequest.UserName);
            if (user == null)
            {
                loginResponse.IsErrorMessage = true;
                loginResponse.Message = $"User '{loginRequest.UserName}' could not be found.";
            }
            else if (!user.Password.Equals(loginRequest.Password))
            {
                loginResponse.IsErrorMessage = true;
                loginResponse.Message = $"Password is incorrect.";
            }
            else
            {
                loginResponse.UserName = user.UserName;
                loginResponse.Name = user.Name;
            }

            return Ok(loginResponse);
        }
    }
}
