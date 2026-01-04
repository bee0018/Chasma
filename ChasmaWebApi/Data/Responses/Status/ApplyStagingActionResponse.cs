using ChasmaWebApi.Data.Objects;

namespace ChasmaWebApi.Data.Responses.Status;

/// <summary>
/// Class representing the components of response to staging/unstaging a file.
/// </summary>
public class ApplyStagingActionResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the status elements.
    /// </summary>
    public List<RepositoryStatusElement> StatusElements { get; set; } = new();
}