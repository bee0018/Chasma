using ChasmaWebApi;
using ChasmaWebApi.Data;
using ChasmaWebApi.Data.Interfaces;
using ChasmaWebApi.Data.Managers;
using ChasmaWebApi.HostedServices;
using ChasmaWebApi.Util;
using Microsoft.EntityFrameworkCore;

string configFilePath = Path.Combine(AppContext.BaseDirectory, "config.xml");
ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath) ?? throw new Exception("Error has occurred deserializing configuration file.");
WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Host.UseWindowsService();
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5000));

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

const string thinClientCorPolicy = "AllowThinClientOriginAndHeaders";
builder.Services.AddCors(options =>
    {
        options.AddPolicy(thinClientCorPolicy, policy =>
        {
            policy.WithOrigins(webApiConfigurations.ThinClientUrl)
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    })
    .AddSingleton(webApiConfigurations)
    .AddSingleton<IPasswordUtility, PasswordUtility>()
    .AddSingleton<ICacheManager, CacheManager>()
    .AddSingleton<IRepositoryConfigurationManager, RepositoryConfigurationManager>()
    .AddSingleton<IRepositoryStatusManager, RepositoryStatusManager>()
    .AddSingleton<IShellManager, ShellManager>()
    .AddEndpointsApiExplorer()
    .AddOpenApiDocument(config =>
    {
        config.Title = "Chasma Git Manager API";
        config.Version = "v1";
    })
    .AddDbContext<ApplicationDbContext>(options => options.UseSqlite(webApiConfigurations.GetDatabaseConnectionString()))
    .AddHostedService<CacheInitializerService>();

WebApplication app = builder.Build();
app.UseCors(thinClientCorPolicy)
    .UseAuthorization()
    .UseOpenApi()
    .UseRouting()
    .UseStaticFiles()
    .UseDefaultFiles()
    .UseHsts()
    .UseHttpsRedirection()
    .UseSwaggerUi();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapControllers();
app.Run();