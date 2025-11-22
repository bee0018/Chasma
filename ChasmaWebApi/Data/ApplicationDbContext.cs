using ChasmaWebApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Data;

/// <summary>
/// Class representing the database context of the Chasma application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// The entity used to represent the user accounts of the Chasma system.
    /// </summary>
    public DbSet<UserAccount> UserAccounts { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}