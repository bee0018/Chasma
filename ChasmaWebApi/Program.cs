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
using System.Diagnostics;
using System.Text;

string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.xml");
ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath) ?? throw new Exception("Error has occurred deserializing configuration file.");
WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(webApiConfigurations.BindingPort));
builder.WebHost.UseWebRoot("wwwroot");

builder.Logging
    .ClearProviders()
    .AddConsole()
    .AddDebug();
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        if (webApiConfigurations.ShowDebugControllers) return;
        
        // Exclude the debug controllers in a production setting.
        List<ExcludeControllerFeatureProvider> controllersToExclude =
        [
            // Example of excluded controller: new(typeof(DebugController))
        ];

        foreach (ExcludeControllerFeatureProvider debugController in controllersToExclude)
        {
            manager.FeatureProviders.Add(debugController);
        }
    });

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "ChasmaWebApi",
            ValidAudience = "ChasmaThinClient",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(webApiConfigurations.JwtSecretKey))
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
app.UseDefaultFiles()
    .UseStaticFiles()
    .UseRouting()
    .UseAuthentication()
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