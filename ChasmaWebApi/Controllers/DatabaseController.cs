using ChasmaWebApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Class representing the debug controller for database CRUD operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        /// <summary>
        /// The database context used for interacting with the database.
        /// </summary>
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseController"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        public DatabaseController(ApplicationDbContext dbContext)
        {
            applicationDbContext = dbContext;
        }

        /// <summary>
        /// Gets all the user accounts from the database.
        /// </summary>
        /// <returns>All the user accounts from the database.</returns>
        [HttpGet]
        public async Task<ActionResult<List<UserAccount>>> GetUserAcccounts()
        {
            return await applicationDbContext.UserAccounts.ToListAsync();
        }

        /// <summary>
        /// Adds a user account to the database.
        /// </summary>
        /// <param name="account">The account to be added to the database.</param>
        /// <returns>Result signifying if the operation was successful or not.</returns>
        [HttpPost]
        [Route("addUserAccount")]
        public async Task<ActionResult> AddUserAccount(UserAccount account)
        {
            await applicationDbContext.UserAccounts.AddAsync(account);
            int rowsAffected = await applicationDbContext.SaveChangesAsync();
            if (rowsAffected > 0)
            {
                return Ok(account);
            }

            return Problem("User could not be added");
        }
    }
}
