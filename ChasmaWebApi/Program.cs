using ChasmaWebApi;
using ChasmaWebApi.Core.Interfaces.Control;
using ChasmaWebApi.Core.Interfaces.Git;
using ChasmaWebApi.Core.Interfaces.Index;
using ChasmaWebApi.Core.Interfaces.Infrastructure;
using ChasmaWebApi.Core.Interfaces.Remote;
using ChasmaWebApi.Core.Interfaces.Simulation;
using ChasmaWebApi.Core.Services.Control;
using ChasmaWebApi.Core.Services.Git;
using ChasmaWebApi.Core.Services.Index;
using ChasmaWebApi.Core.Services.Infrastructure;
using ChasmaWebApi.Core.Services.Remote;
using ChasmaWebApi.Core.Services.Simulation;
using ChasmaWebApi.Data;
using ChasmaWebApi.HostedServices;
using ChasmaWebApi.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

string appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Chasma");
string logPath = Path.Combine(
    appDataDirectory,
    "logs",
    "log-.log");
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
    string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "config.xml");
    bool isDevelopment = builder.Environment.IsDevelopment();
    string configFilePath = isDevelopment
        ? defaultConfigPath
        : Path.Combine(appDataDirectory, "config.xml");
    if (!File.Exists(configFilePath) && !isDevelopment)
    {
        // If the config file doesn't exist, copy the default one to the app data directory.
        // This ensures that the application has a config file to read from and write to, while also allowing for user modifications in production.
        Directory.CreateDirectory(appDataDirectory);
        File.Copy(defaultConfigPath, configFilePath);
    }

    ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath) ?? throw new Exception("Error has occurred deserializing configuration file.");
    if (IsPortInUse(webApiConfigurations.BindingPort))
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = $"http://localhost:{webApiConfigurations.BindingPort}",
            UseShellExecute = true
        };
        Process.Start(startInfo);
        return;
    }

    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(webApiConfigurations.BindingPort));
    builder.WebHost.UseWebRoot("wwwroot");

    builder.Host.UseSerilog();
    if (OperatingSystem.IsWindows())
    {
        builder.Logging.AddEventLog();
    }

    Log.Information("Starting Chasma Web API...");
    Log.Information("Environment: {Env}", builder.Environment.EnvironmentName);
    Log.Information("Using config file: {ConfigPath}", configFilePath);
    builder.Services.AddControllers();
    string devCorsPolicy = "DevCors";
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(devCorsPolicy, policy =>
        {
            policy
                .WithOrigins(webApiConfigurations.ThinClientUrl)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
    builder.Services
        .AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            string jwtKey = !string.IsNullOrEmpty(webApiConfigurations.JwtSecretKey) && webApiConfigurations.JwtSecretKey.Length >= 16
                ? webApiConfigurations.JwtSecretKey
                : ChasmaWebApiConfigurations.DefaultJwtSecretKey;
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "ChasmaWebApi",
                ValidAudience = "ChasmaThinClient",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services
        .AddSingleton(webApiConfigurations)
        .AddSingleton<IPasswordUtility, PasswordUtility>()
        .AddSingleton<ICacheManager, CacheManager>()
        .AddSingleton<IApplicationControlService, ApplicationControlService>()
        .AddSingleton<IGitBranchService, GitBranchService>()
        .AddSingleton<IGitRepositoryService, GitRepositoryService>()
        .AddSingleton<IGitStashService, GitStashService>()
        .AddSingleton<IRepositoryIndexService, RepositoryIndexService>()
        .AddSingleton<IShellExecutionService, ShellExecutionService>()
        .AddSingleton<IGitHubService, GitHubService>()
        .AddSingleton<IGitLabService, GitLabService>()
        .AddSingleton<ISimulationService, SimulationService>()
        .AddEndpointsApiExplorer()
        .AddOpenApiDocument(config =>
        {
            config.Title = "Chasma Git Manager API";
            config.Version = "v1";
        })
        .AddDbContext<ApplicationDbContext>(options => options.UseSqlite(webApiConfigurations.GetDatabaseConnectionString()))
        .AddHostedService<CacheInitializerService>();

    WebApplication app = builder.Build();
    using (IServiceScope scope = app.Services.CreateScope())
    {
        ApplicationDbContext databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await databaseContext.Database.MigrateAsync();
    }

    app.UseDefaultFiles()
        .UseStaticFiles()
        .UseRouting();
    if (app.Environment.IsDevelopment())
    {
        app.UseCors(devCorsPolicy);
    }

    app.UseAuthentication()
        .UseAuthorization()
        .UseOpenApi();
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage()
        .UseSwaggerUi();
    }

    app.MapControllers();
    app.MapFallbackToFile("index.html");

    // Open the default browser after a short delay to ensure the server is up and running.
    await Task.Run(() =>
    {
        try
        {
            Thread.Sleep(1000);
            ProcessStartInfo startInfo = new()
            {
                FileName = $"http://localhost:{webApiConfigurations.BindingPort}",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch { }
    });
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