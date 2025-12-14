using LibGit2Sharp;

namespace ChasmaWebApi.Data.Objects;

/// <summary>
/// Class representing the properties of a local git repository on the local machine.
/// </summary>
public class LocalGitRepository
{
    /// <summary>
    /// Gets or sets the name of the repository.
    /// </summary>
    public string RepositoryName { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the repository owner.
    /// </summary>
    public string RepositoryOwner { get; set; }
    
    /// <summary>
    /// Gets or sets the local git repository.
    /// </summary>
    public Repository Repository { get; set; }
}