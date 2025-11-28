namespace ChasmaWebApi.Requests;

/// <summary>
/// Class representing the request to encode a JSON Web Token.
/// </summary>
public class EncodeJwtRequest
{
    /// <summary>
    /// Gets or sets the secret key used to sign the JSON web token.
    /// </summary>
    public required string SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the username to include in the JSON web token.
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the name to include in the JSON web token.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the role to include in the JSON web token.
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Gets or sets the audience for the JSON web token.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Gets or sets the issuer for the JSON web token.
    /// </summary>
    public required string Issuer { get; set; }

    /// <summary>
    /// Gets or sets any custom claim types to include in the JSON web token.
    /// </summary>
    public List<string> CustomClaimTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets any custom claim values to include in the JSON web token.
    /// </summary>
    public List<string> CustomClaimValues { get; set; } = new();

    /// <summary>
    /// Gets or sets the expiration time in minutes for the JSON web token.
    /// </summary>
    public required int ExpireInMinutes { get; set; }
}