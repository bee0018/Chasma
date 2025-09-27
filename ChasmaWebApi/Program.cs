
using ChasmaWebApi;
using ChasmaWebApi.Util;
using Microsoft.EntityFrameworkCore;

string configFilePath = "config.xml";
ChasmaWebApiConfigurations? webApiConfigurations = ChasmaXmlBase.DeserializeFromFile<ChasmaWebApiConfigurations>(configFilePath);
if (webApiConfigurations == null)
{
    Console.WriteLine("Error has occurred deserializing configuration file.");
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Services.AddControllers();
builder.Services
    .AddSingleton(webApiConfigurations)
    .AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(webApiConfigurations.ThinClientUrl)
            .AllowAnyMethod()
            .WithHeaders("Content-Type", "Authorization");
        });
    })
    .AddEndpointsApiExplorer()
    .AddOpenApiDocument(config =>
    {
        config.Title = "Chasma Web API";
        config.Version = "v1";
    })
    .AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(webApiConfigurations.DatabaseConfigurations.GetConnectionString()));

WebApplication app = builder.Build();
app.UseCors()
    .UseAuthorization()
    .UseOpenApi()
    .UseRouting()
    .UseStaticFiles()
    .UseDefaultFiles()
    .UseHsts()
    .UseHttpsRedirection();
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage()
        .UseSwaggerUi();
}

app.Run();