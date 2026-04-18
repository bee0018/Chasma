using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Data.Messages.Application;
using ChasmaWebApi.Data.Requests.Configuration;
using ChasmaWebApi.Data.Responses.Configuration;
using ChasmaWebApi.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChasmaWebApi.Controllers
{
    /// <summary>
    /// Controller for handling application configuration related API endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationConfigurationController : ControllerBase
    {
        /// <summary>
        /// The internal web API configurations.
        /// </summary>
        private readonly ChasmaWebApiConfigurations apiConfiguration;

        /// <summary>
        /// The logger instance for logging diagnostic and operational information within the class.
        /// </summary>
        private readonly ILogger<ApplicationConfigurationController> logger;

        /// <summary>
        /// The internal API application control service for managing application-level operations.
        /// </summary>
        private readonly IApplicationControlService applicationControlService;

        /// <summary>
        /// The web host environment, which provides information about the hosting environment the application is running in.
        /// </summary>
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationConfigurationController"/> class with the specified API configurations.
        /// </summary>
        /// <param name="config">The internal API configuration.</param>
        /// <param name="log">The logger instance.</param>
        /// <param name="controlSerivce">The application control service instance.</param>
        /// <param name="env">The web host environment instance.</param>
        public ApplicationConfigurationController(ChasmaWebApiConfigurations config, ILogger<ApplicationConfigurationController> log, IApplicationControlService controlSerivce, IWebHostEnvironment env)
        {
            apiConfiguration = config;
            logger = log;
            applicationControlService = controlSerivce;
            webHostEnvironment = env;
        }

        /// <summary>
        /// Gets a message indicating whether the system is ready.
        /// Note: The is determined by checking if all the required XML elements are present and valid.
        /// </summary>
        /// <returns>Response indicating whether the system is ready.</returns>
        [HttpGet("systemReady")]
        [AllowAnonymous]
        public ActionResult<GetSystemReadyMessage> GetSystemReady()
        {
            List<string> invalidElements = GetInvalidRequiredXmlElements(apiConfiguration);
            GetSystemReadyMessage response = new()
            {
                IsReady = invalidElements.Count == 0,
                InvalidElements = string.Join(", ", invalidElements)
            };
            return Ok(response);
        }

        /// <summary>
        /// Modifies the API configuration based on the request.
        /// </summary>
        /// <param name="request">The request containing the new API configuration.</param>
        /// <returns>The response indicating the result of the modification.</returns>
        [HttpPost("modifyConfig")]
        [AllowAnonymous]
        public ActionResult<ModifyApiConfigResponse> ModifyConfig([FromBody] ModifyApiConfigRequest request)
        {
            ModifyApiConfigResponse response = new();
            string requestName = nameof(ModifyApiConfigRequest);
            if (request == null)
            {
                logger.LogError("Received a null {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Request must be populated.";
                return BadRequest(response);
            }

            ChasmaWebApiConfigurations newConfig = request.ApiConfiguration;
            if (newConfig == null)
            {
                logger.LogError("Received a null ApiConfiguration in the {request}. Sending error response.", requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "API configuration must be populated.";
                return Ok(response);
            }

            List<string> invalidElements = GetInvalidRequiredXmlElements(newConfig);
            if (invalidElements.Count > 0)
            {
                logger.LogError("Received an invalid ApiConfiguration in the {request}. Invalid elements: {invalidElements}. Sending error response.", requestName, string.Join(", ", invalidElements));
                response.IsErrorResponse = true;
                response.ErrorMessage = $"Invalid configuration elements: {string.Join(", ", invalidElements)}";
                return Ok(response);
            }

            bool isDevelopment = webHostEnvironment.IsDevelopment();
            string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
            string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Chasma");
            string configFilePath = isDevelopment
                ? defaultConfigPath
                : Path.Combine(appDataDirectory, "config.xml");
            ChasmaWebApiConfigurations currentConfig = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath);
            if (currentConfig == null)
            {
                logger.LogError("Failed to read the current API configuration from {configFilePath} during {request}. Sending error response.", configFilePath, requestName);
                response.IsErrorResponse = true;
                response.ErrorMessage = "Failed to read the current API configuration.";
                return Ok(response);
            }

            applicationControlService.UpdateApiConfiguration(configFilePath, newConfig, currentConfig);
            logger.LogInformation("Successfully processed {request}. Sending success response.", requestName);
            return Ok(response);
        }

        #region Private Methods

        /// <summary>
        /// Gets the invalid XML elements in the configuration, if any. This is used to determine if the system is ready.
        /// </summary>
        /// <param name="config">The API configurations.</param>
        /// <returns>The list of invalid elements.</returns>
        private static List<string> GetInvalidRequiredXmlElements(ChasmaWebApiConfigurations config)
        {
            List<string> invalidElements = [];
            if (!IsValidUrl(config.WebApiUrl))
            {
                invalidElements.Add("webApiUrl");
            }

            if (!IsValidUrl(config.ThinClientUrl))
            {
                invalidElements.Add("thinClientUrl");
            }

            if (!IsJwtSecretKeyValid(config.JwtSecretKey))
            {
                invalidElements.Add("jwtSecretKey");
            }

            if (config.BindingPort <= 0 || config.BindingPort > 65535)
            {
                invalidElements.Add("bindingPort");
            }

            return invalidElements;
        }

        /// <summary>
        /// Detemines if the url provided is a valid HTTP or HTTPS url.
        /// </summary>
        /// <param name="url">The url to validate.</param>
        /// <returns>True if valid; false otherwise.</returns>
        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Determines if the JWT secret key is valid.
        /// </summary>
        /// <param name="jwtSecretKey">The secret key to validate.</param>
        /// <returns>True if valid; false otherwise.</returns>
        private static bool IsJwtSecretKeyValid(string jwtSecretKey)
        {
            // For demonstration purposes, we will consider a valid JWT secret key to be at least 16 characters long.
            // In a real application, you would want to implement a more robust validation mechanism.
            return (!string.IsNullOrEmpty(jwtSecretKey) && jwtSecretKey.Length >= 16) || jwtSecretKey == ChasmaWebApiConfigurations.DefaultJwtSecretKey;
        }

        #endregion
    }
}
