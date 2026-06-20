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
using ChasmaWebApi.Hubs;
using ChasmaWebApi.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ChasmaWebApi.Core.Services
{
    /// <summary>
    /// Class representing the service factory of the application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// The development CORS policy.
        /// </summary>
        private static readonly string devCorsPolicy = "DevCors";

        /// <summary>
        /// Setups the web application builder.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="webApiConfigurations">The API configuration.</param>
        /// <returns>The configured web application builder.</returns>
        public static WebApplicationBuilder SetupBuilder(this WebApplicationBuilder builder, ChasmaWebApiConfigurations webApiConfigurations)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                X509Certificate2 localCertificate = CertificateUtility.GetOrCreateLocalCertificate();
                int httpPort = webApiConfigurations.BindingPort;
                int httpsPort = webApiConfigurations.SecureBindingPort;
                options.ListenLocalhost(httpPort);
                options.ListenLocalhost(httpsPort, listenOptions =>
                {
                    listenOptions.UseHttps(localCertificate);
                });
            });
            builder.WebHost.UseWebRoot("wwwroot");
            builder.Host.UseSerilog();
            if (OperatingSystem.IsWindows())
            {
                builder.Logging.AddEventLog();
            }

            return builder;
        }

        /// <summary>
        /// Adds the application services to the service collection.
        /// </summary>
        /// <param name="services">The application's service collection.</param>
        /// <param name="webApiConfigurations">The API configuration.</param>
        /// <returns>The application service collection with the added resources.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, ChasmaWebApiConfigurations webApiConfigurations)
        {
            services.AddControllers();
            services.AddSignalR();
            services.AddCors(options =>
            {
                options.AddPolicy(devCorsPolicy, policy =>
                {
                    policy
                        .WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            services
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

            return services
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
                    config.Title = "Emryce API";
                    config.Version = "v1";
                })
                .AddDbContext<ApplicationDbContext>(options => options.UseSqlite(webApiConfigurations.GetDatabaseConnectionString()))
                .AddHostedService<CacheInitializerService>()
                .AddHostedService<SystemUpdaterService>();
        }

        /// <summary>
        /// Applies the application services to the application builder.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The configured application builder.</returns>
        public static async Task<WebApplication> UseApplicationServices(this WebApplication app)
        {
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

            app.MapHub<NotificationHub>("/notificationHub");
            app.MapControllers();
            app.MapFallbackToFile("index.html");
            return app;
        }
    }
}
