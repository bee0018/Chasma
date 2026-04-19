using ChasmaWebApi;
using ChasmaWebApi.Core.Services;
using ChasmaWebApi.Util;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;

string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Chasma");
string logPath = Path.Combine(
    appDataDirectory,
    "logs",
    "chasma-.log");
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
try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    bool isDevelopment = builder.Environment.IsDevelopment();
    HandleConfigurationFileSetup(appDataDirectory, isDevelopment);
    string configFilePath = ChasmaWebApiConfigurations.GetConfigXmlFilePath(isDevelopment);
    ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath) ?? throw new Exception("Error has occurred deserializing configuration file.");
    if (IsPortInUse(webApiConfigurations.BindingPort))
    {
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
    LaunchStartupGate(webApiConfigurations.BindingPort);
    app.Run();
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

#endregion