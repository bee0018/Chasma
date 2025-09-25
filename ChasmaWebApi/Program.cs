
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
builder.Services.AddCors()
    .AddSingleton(webApiConfigurations)
    .AddEndpointsApiExplorer()
    .AddOpenApiDocument(config =>
    {
        config.Title = "Chasma Web API";
        config.Version = "v1";
    })
    .AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(webApiConfigurations.DatabaseConfigurations.GetConnectionString()));

WebApplication app = builder.Build();
app.UseCors(i => i.AllowAnyOrigin())
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