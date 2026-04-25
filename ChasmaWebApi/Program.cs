using ChasmaWebApi;
using ChasmaWebApi.Core.Services;
using ChasmaWebApi.Util;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Chasma");
string logPath = Path.Combine(appDataDirectory, "logs", "chasma-.log");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.Async(a => a.File(
    logPath,
    rollingInterval: RollingInterval.Day,
    fileSizeLimitBytes: 10 * 1024 * 1024,
    retainedFileCountLimit: 7,
    rollOnFileSizeLimit: true))
    .CreateLogger();
ChasmaWebApiConfigurations? webApiConfigurations = null;
bool isDevelopment = false;
try
{
    int attempts = 0;
    while (attempts < 5)
    {
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            isDevelopment = builder.Environment.IsDevelopment();
            HandleConfigurationFileSetup(appDataDirectory, isDevelopment);
            string configFilePath = ChasmaWebApiConfigurations.GetConfigXmlFilePath(isDevelopment);
            webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath) ?? throw new Exception("Error has occurred deserializing configuration file.");
            if (webApiConfigurations == null)
            {
                throw new Exception("Failed to load configuration. Please ensure the configuration file is present and valid.");
            }

            if (IsPortInUse(webApiConfigurations.BindingPort) && await IsOurApiRunning(webApiConfigurations.BindingPort))
            {
                // An instance of the API is already running on the specified port, so we can skip starting a new one and just open the browser to the existing instance.
                LaunchStartupGate(webApiConfigurations.BindingPort);
                return;
            }

            Log.Information("Starting Chasma Web API...");
            Log.Information("Environment: {Env}", builder.Environment.EnvironmentName);
            Log.Information("Using config file: {ConfigPath}", configFilePath);

            builder.SetupBuilder(webApiConfigurations);
            builder.Services.AddApplicationServices(webApiConfigurations);
            WebApplication app = builder.Build();
            await app.UseApplicationServices();
            await app.StartAsync();
            LaunchStartupGate(webApiConfigurations.BindingPort);
            await app.WaitForShutdownAsync();
        }
        catch (IOException exception)
        {
            if (exception.InnerException is SocketException socketException && socketException.SocketErrorCode != SocketError.AddressAlreadyInUse)
            {
                // If it's not an address in use error, rethrow it.
                throw;
            }

            if (webApiConfigurations == null)
            {
                Log.Fatal("Failed to load configuration. Application will exit.");
                return;
            }

            Log.Warning("Port {Port} is already in use. Attempting to use a different port...", webApiConfigurations.BindingPort);
            HandlePortBindingFailure(webApiConfigurations, isDevelopment);
            attempts++;
        }
    }

    Log.Fatal("Failed to bind to a port after multiple attempts. Application will exit.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during application startup. Shutting down...");
}
finally
{
    Log.CloseAndFlush();
}

#region Private Methods

/// <summary>
/// Determines if the port is already in use.
/// </summary>
/// <param name="port">The port to run on.</param>
static bool IsPortInUse(int port)
{
    IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
    IPEndPoint[] tcpListeners = ipProperties.GetActiveTcpListeners();
    IPEndPoint[] udpListeners = ipProperties.GetActiveUdpListeners();
    return tcpListeners.Any(i => i.Port == port) || udpListeners.Any(i => i.Port == port);
}

/// <summary>
/// Launches the user interface startup gate.
/// Note: The startup gate is responsible for prompting the user to setup the application or directly to login.
/// </summary>
/// <param name="port">The port to launch application from.</param>
static async void LaunchStartupGate(int port)
{
    // Open the default browser after a short delay to ensure the server is up and running.
    await Task.Run(() =>
    {
        try
        {
            Thread.Sleep(1000);
            ProcessStartInfo startInfo = new()
            {
                FileName = $"http://localhost:{port}",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch { }
    });
}

/// <summary>
/// Handles the configuration file setup.
/// </summary>
/// <param name="appDataDirectory">The application data directly.</param>
/// <param name="isDevelopment">Flag indicating whether the application is in development mode.</param>
static void HandleConfigurationFileSetup(string appDataDirectory, bool isDevelopment)
{
    string configFilePath = ChasmaWebApiConfigurations.GetConfigXmlFilePath(isDevelopment);
    if (!File.Exists(configFilePath) && !isDevelopment)
    {
        // If the config file doesn't exist, copy the default one to the app data directory.
        // This ensures that the application has a config file to read from and write to, while also allowing for user modifications in production.
        Directory.CreateDirectory(appDataDirectory);
        string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
        File.Copy(defaultConfigPath, configFilePath);
    }
}

/// <summary>
/// Gets the free TCP port. It will be decided by the OS.
/// </summary>
/// <returns>The free port to use.</returns>
static int GetFreePort()
{
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

/// <summary>
/// Determines if the API is running on the specified port.
/// </summary>
/// <param name="port">The port to launch application from.</param>
static async Task<bool> IsOurApiRunning(int port)
{
    try
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync($"https://localhost:{port}/api/Health/heartbeat"); // or version endpoint
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}

/// <summary>
/// Handles the port binding failure by assigning a free port and updating the configuration file.
/// </summary>
/// <param name="config">The configuration object containing the port information.</param>
/// <param name="isDevelopment">Flag indicating whether the application is in development mode.</param>
static void HandlePortBindingFailure(ChasmaWebApiConfigurations config, bool isDevelopment)
{
    int freePort = GetFreePort();
    config.BindingPort = freePort;
    string xmlText = ChasmaXmlBase.GenerateXml(config);
    string configFilePath = ChasmaWebApiConfigurations.GetConfigXmlFilePath(isDevelopment);
    File.WriteAllText(configFilePath, xmlText, Encoding.UTF8);
}

#endregion