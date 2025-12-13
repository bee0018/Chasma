using System.IdentityModel.Tokens.Jwt;

namespace ChasmaWebApi.Data.Responses;

/// <summary>
/// Class representing the contents of the decoded JSON Web token.
/// </summary>
public class DecodeJwtResponse : ResponseBase
{
    /// <summary>
    /// Gets or sets the decoded JWT.
    /// </summary>
    public string DecodedJwtTokenString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the token is valid.
    /// </summary>
    public bool IsValidToken { get; set; }
    
    /// <summary>
    /// Gets or sets the JwtHeader on the decoded security token.
    /// </summary>
    public JwtHeader Header { get; set; }
    
    /// <summary>
    /// Gets or sets the JwtPayload on the decoded security token.
    /// </summary>
    public JwtPayload Payload { get; set; }

    /// <summary>
    /// Gets or sets all claim types to include in the JSON web token.
    /// </summary>
    public List<string> ClaimTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets all claim values to include in the JSON web token.
    /// </summary>
    public List<string> ClaimValues { get; set; } = new();

}
