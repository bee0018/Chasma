using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Models;
using ChasmaWebApi.Data.Objects.Application;
using ChasmaWebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;

namespace ChasmaWebApi.HostedServices
{
    /// <summary>
    /// Class representing a hosted service that is responsible for updating the system's software version.
    /// </summary>
    /// <param name="logger">The internal logging interface.</param>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="notificationHubContext">The notification Hub context.</param>
    /// <param name="webEnvironment">The web host environment.</param>
    public class SystemUpdaterService(ILogger<SystemUpdaterService> logger, IServiceScopeFactory serviceScopeFactory, IHubContext<NotificationHub> notificationHubContext, IWebHostEnvironment webEnvironment) : IHostedService
    {
        #region Private Properties/Fields

        /// <summary>
        /// Gets or sets the timer used to poll for system software updates.
        /// </summary>
        private PeriodicTimer UpdatePollTimer { get; set; }

        /// <summary>
        /// Gets or sets the HTTP client used for downloading and retrieving resources.
        /// </summary>
        private HttpClient WebClient { get; set; }

        /// <summary>
        /// The internal logging interface.
        /// </summary>
        private readonly ILogger<SystemUpdaterService> logger = logger;

        /// <summary>
        /// The internal service scope factory.
        /// </summary>
        private readonly IServiceScopeFactory scopeFactory = serviceScopeFactory;

        /// <summary>
        /// The notification hub context for sending out real-time updates.
        /// </summary>
        private readonly IHubContext<NotificationHub> hubContext = notificationHubContext;

        /// <summary>
        /// The web host environment data.
        /// </summary>
        private readonly IWebHostEnvironment webHostEnvironment = webEnvironment;

        /// <summary>
        /// Flag indicating whether the application assembly could not be found.
        /// </summary>
        private bool assemblyNotFound;

        /// <summary>
        /// Flag indicating whether the application version could not be found.
        /// </summary>
        private bool systemVersionNotFound;

        /// <summary>
        /// The websocket hub context method.
        /// </summary>
        private const string websocketMethod = "OnUpdateDownloaded";

        #endregion

        // <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (webHostEnvironment.IsDevelopment())
            {
                // Don't want to interact with live server unless in a production setting.
                return;
            }

            WebClient = new() { Timeout = TimeSpan.FromMinutes(30) };
            await StartSystemUpdatePollingAsync(cancellationToken);
        }

        // <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping the System Updater hosted service and disposing timers.");
            UpdatePollTimer?.Dispose();
            WebClient?.Dispose();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Starts the polling cycle of downloading applicable system updates the user's machine.
        /// </summary>
        /// <param name="cancellationToken">The cancellation.</param>
        /// <returns>The polling task.</returns>
        private async Task StartSystemUpdatePollingAsync(CancellationToken cancellationToken)
        {
            UpdatePollTimer = new PeriodicTimer(TimeSpan.FromDays(7));
            _ = Task.Run(async () =>
            {
                while (await UpdatePollTimer.WaitForNextTickAsync(cancellationToken))
                {
                    try
                    {
                        Version currentSystemVersion = GetSystemCurrentApplicationVersion();
                        if (currentSystemVersion == null)
                        {
                            systemVersionNotFound = true;
                            logger.LogError("Could not find application version so updates will not be polled.");
                            break;

                        }

                        SystemManifest manifest = await GetSystemManifestFromRemoteHost(cancellationToken);
                        if (manifest == null)
                        {
                            logger.LogError("Could not retrieve system manifest from remote host.");
                            continue;
                        }

                        if (!Version.TryParse(manifest.Version, out Version manifestVersion))
                        {
                            logger.LogError("The newest system manifest version could not be determined.");
                            continue;
                        }

                        if (manifestVersion <= currentSystemVersion)
                        {
                            // Nothing to do because the software is up to date.
                            continue;
                        }

                        using (IServiceScope scope = scopeFactory.CreateScope())
                        {
                            ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            bool alreadyDownloaded = await databaseContext.SystemManifests.AnyAsync(i => Version.Parse(i.Version) == manifestVersion, cancellationToken);
                            if (alreadyDownloaded)
                            {
                                // Sending notification to client that system update has already been downloaded from the remote host, but not installed.
                                await hubContext.Clients.All.SendAsync(websocketMethod, manifest, cancellationToken);
                                continue;
                            }

                            await DownloadSystemUpdateAsync(manifest, databaseContext, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error while polling system updates: {error}", ex);
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Gets the system version for this instance.
        /// </summary>
        /// <returns>The instance version.</returns>
        private Version GetSystemCurrentApplicationVersion()
        {
            if (assemblyNotFound || systemVersionNotFound)
            {
                return null;
            }

            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                assemblyNotFound = true;
                logger.LogError("Could not find application assembly so updates will not be polled.");
                return null;
            }

            AssemblyName assemblyName = assembly.GetName();
            return assemblyName.Version;
        }

        /// <summary>
        /// Gets the system manifest from the remote host.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task containing the system manifest data.</returns>
        private async Task<SystemManifest?> GetSystemManifestFromRemoteHost(CancellationToken cancellationToken)
        {
            try
            {
                string manifestUrl = "https://download.emryce.com/official-releases/latest.json";
                SystemManifest manifest = await WebClient.GetFromJsonAsync<SystemManifest>(manifestUrl, cancellationToken);
                return manifest;
            }
            catch (Exception error)
            {
                logger.LogError("Error when retrieving system manifest from remote host: {error}", error.Message);
                return null;
            }
        }

        /// <summary>
        /// Downloads the system update from the remote host.
        /// </summary>
        /// <param name="manifest">The system manifest.</param>
        /// <param name="databaseContext">The internal database context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task downloading the update file to the user's filesystem.</returns>
        private async Task DownloadSystemUpdateAsync(SystemManifest manifest, ApplicationDbContext databaseContext, CancellationToken cancellationToken)
        {
            string appUpdatesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Emryce", "Updates", manifest.Version);
            try
            {
                HttpResponseMessage httpMessage = await WebClient.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                httpMessage.EnsureSuccessStatusCode();
                using Stream remoteFileStream = await httpMessage.Content.ReadAsStreamAsync(cancellationToken);
                if (!Directory.Exists(appUpdatesDirectory))
                {
                    Directory.CreateDirectory(appUpdatesDirectory);
                }

                Uri uri = new(manifest.DownloadUrl);
                string filePath = Path.GetFileName(uri.AbsolutePath);
                string downloadFilePath = Path.Combine(appUpdatesDirectory, filePath);
                using FileStream localFileStream = File.Create(downloadFilePath);
                await remoteFileStream.CopyToAsync(localFileStream, cancellationToken);
                if (!IsDownloadedUpdateFileValid(filePath, manifest.Checksum))
                {
                    throw new Exception("Downloaded file has been tampered or corrupted.");
                }

                logger.LogInformation("Successfully downloaded system update version {version} located {location}", manifest.Version, appUpdatesDirectory);
                SystemManifestModel manifestModel = new() { Version = manifest.Version };
                await databaseContext.AddAsync(manifestModel, cancellationToken);
                await databaseContext.SaveChangesAsync(cancellationToken);
                await hubContext.Clients.All.SendAsync(websocketMethod, manifest, cancellationToken);
            }
            catch (Exception error)
            {
                logger.LogError("Error when download system update version {version} from remote host: {error}", manifest.Version, error.Message);
                if (Directory.Exists(appUpdatesDirectory))
                {
                    logger.LogError("Removing erroneous download artifacts from filesystem.");
                    Directory.Delete(appUpdatesDirectory, true);
                }
            }
        }

        /// <summary>
        /// Determines if the downloaded update file is valid.
        /// </summary>
        /// <param name="filePath">The file path to the update file.</param>
        /// <param name="expectedHash">The expected SHA256 file hash.</param>
        /// <returns>True if the calculated hash matches the expected hash; false otherwise.</returns>
        private static bool IsDownloadedUpdateFileValid(string filePath, string expectedHash)
        {
            using SHA256 sha256 = SHA256.Create();
            using FileStream fileStream = File.OpenRead(filePath);
            byte[] hashBytes = sha256.ComputeHash(fileStream);
            string calculatedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return string.Equals(calculatedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
