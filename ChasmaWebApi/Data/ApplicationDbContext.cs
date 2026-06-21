using ChasmaWebApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChasmaWebApi.Data;

/// <summary>
/// Class representing the database context of the Emryce application.
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the entity set used to represent the user accounts of application.
    /// </summary>
    public DbSet<UserAccountModel> UserAccounts { get; set; }

    /// <summary>
    /// Gets or sets the entity set used to represent the repositories maintained by the users in the system.
    /// </summary>
    public DbSet<RepositoryModel> Repositories { get; set; }

    /// <summary>
    /// Gets or sets the entity set used to represent the working directories maintained by the users in the system.
    /// </summary>
    public DbSet<WorkingDirectoryModel> WorkingDirectories { get; set; }

    /// <summary>
    /// Gets or sets the work context snapshots of the development workspaces maintained by the users in the system.
    /// </summary>
    public DbSet<WorkContextSnapshotModel> WorkContextSnapshots { get; set; }

    /// <summary>
    /// Gets or sets the repository workspace context snapshots.
    /// </summary>
    public DbSet<RepositoryWorkContextSnapshotModel> RepositorySnapshots { get; set; }

    /// <summary>
    /// Gets or sets the system updates.
    /// </summary>
    public DbSet<SystemManifestModel> SystemManifests { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context configuration options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}