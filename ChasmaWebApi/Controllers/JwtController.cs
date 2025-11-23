using ChasmaWebApi.Requests;
using ChasmaWebApi.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ChasmaWebApi.Data;

namespace ChasmaWebApi.Controllers;

/// <summary>
/// Class containing the routes for manipulating JSON Web Tokens.
/// </summary>
[Route("api")]
public class JwtController : ControllerBase
{
    /// <summary>
    /// The internal logger for logging status.
    /// </summary>
    private readonly ILogger<JwtController> logger;

    /// <summary>
    /// The JWT security token handler.
    /// </summary>
    private readonly JwtSecurityTokenHandler tokenHandler;

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtController"/> class.
    /// </summary>
    /// <param name="log">The injected internal logger.</param>
    public JwtController(ILogger<JwtController> log)
    {
        logger = log;
        tokenHandler = new JwtSecurityTokenHandler();
    }
    
    #endregion
    
    /// <summary>
    /// Encodes the JWT sent by the client.
    /// </summary>
    /// <param name="encodeJwtRequest">The request to encode the JWT.</param>
    /// <returns>Result signifying if the operation was successful or not.</returns>
    [HttpPost]
    [Route("encodeJwt")]
    public ActionResult<EncodeJwtResponse> EncodeJwt([FromBody] EncodeJwtRequest encodeJwtRequest)
    {
        EncodeJwtResponse encodeJwtResponse = new();
        if (encodeJwtRequest is null)
        {
            logger.LogError("Cannot encode JWT since the request has null data.");
            encodeJwtResponse.IsErrorResponse = true;
            encodeJwtResponse.ErrorMessage = "Cannot encode JWT since the request has null data.";
            return BadRequest(encodeJwtResponse);
        }

        string username = encodeJwtRequest.Username;
        byte[] secretInBytes = System.Text.Encoding.UTF8.GetBytes(encodeJwtRequest.SecretKey);
        SymmetricSecurityKey securityKey = new(secretInBytes);
        SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);
        List<Claim> claims =
        [
           new(JwtRegisteredClaimNames.Sub, username),
           new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        if (encodeJwtRequest.CustomClaims is not null)
        {
            List<Claim> customClaims = encodeJwtRequest.CustomClaims
                .Select(claim => new Claim(claim.Key, claim.Value))
                .ToList();
            claims.AddRange(customClaims);
        }

        DateTime expirationTime = DateTime.UtcNow.AddMinutes(encodeJwtRequest.ExpireInMinutes);
        JwtSecurityToken token = new(
            issuer: encodeJwtRequest.Issuer,
            audience: encodeJwtRequest.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expirationTime,
            signingCredentials: signingCredentials);

       try
       {
            string encodedToken = tokenHandler.WriteToken(token);
            encodeJwtResponse = new EncodeJwtResponse
            {
                Token = encodedToken,
                ExpirationTime = expirationTime,
            };
            logger.LogInformation("Successfully created an encoded JWT token.");
            return Ok(encodeJwtResponse);
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while encoding the jwt request: {errorMessage}", ex.Message);
            encodeJwtResponse.IsErrorResponse = true;
            encodeJwtResponse.ErrorMessage = ex.Message;
            return Ok(encodeJwtResponse);
        }
    }

    /// <summary>
    /// Decodes the JWT sent by the client.
    /// </summary>
    /// <param name="decodeJwtRequest">The request to decode the JWT.</param>
    /// <returns>Result signifying if the operation was successful or not.</returns>
    [HttpPost]
    [Route("decodeJwt")]
    public ActionResult<DecodeJwtResponse> DecodeJwt([FromBody] DecodeJwtRequest decodeJwtRequest)
    {
        if (string.IsNullOrEmpty(decodeJwtRequest.EncodedToken))
        {
            logger.LogError("Cannot decode JWT since the request has null data.");
            return BadRequest("There is no data to process!");
        }

        byte[] secretInBytes = System.Text.Encoding.UTF8.GetBytes(decodeJwtRequest.SecretKey);
        SymmetricSecurityKey securityKey = new(secretInBytes);
        TokenValidationParameters tokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = decodeJwtRequest.Issuer,
            ValidateAudience = true,
            ValidAudience = decodeJwtRequest.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        DecodeJwtResponse decodeJwtResponse = new();
        string errorMessage;
        try
        {
            string encodedJwt = decodeJwtRequest.EncodedToken;
            tokenHandler.ValidateToken(decodeJwtRequest.EncodedToken, tokenValidationParameters, out SecurityToken validatedToken);
            logger.LogInformation("Token is valid with identifier: {userId}", validatedToken.Id);
            JwtSecurityToken decodedJwt = tokenHandler.ReadJwtToken(encodedJwt);
            decodeJwtResponse.IsValidToken = true;
            decodeJwtResponse.DecodedJwtTokenString = decodedJwt.ToString();
            decodeJwtResponse.Header = decodedJwt.Header;
            decodeJwtResponse.Payload =  decodedJwt.Payload;
            return Ok(decodeJwtResponse);
        }
        catch (SecurityTokenExpiredException)
        {
            errorMessage = "Token is invalid because the it is expired.";
            logger.LogError(errorMessage);
            decodeJwtResponse.IsErrorResponse = true;
            decodeJwtResponse.ErrorMessage = errorMessage;
            return Ok(decodeJwtResponse);
        }
        catch (SecurityTokenValidationException)
        {
            errorMessage = "Token validation failed (e.g., invalid signature or claims).";
            logger.LogError(errorMessage);
            decodeJwtResponse.IsErrorResponse = true;
            decodeJwtResponse.ErrorMessage = errorMessage;
            return Ok(decodeJwtResponse);
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred: {ex.Message}.");
            decodeJwtResponse.IsErrorResponse = true;
            decodeJwtResponse.ErrorMessage = "An unexpected error occurred. Check server logs.";
            return Ok(decodeJwtResponse);
        }
    }
}