using Microsoft.OpenApi;
using ProdHelperService;
using ProdHelperService.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "RELAY_");

builder.Services.AddProdHelperControllers();
builder.Services.AddHostedService<RelayListenerHostedService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProdHelperService API",
        Version = "v1",
        Description = "Local-only documentation and test surface for the same controllers " +
                      "the Azure Relay listener dispatches to. Calling an endpoint here does " +
                      "not go through Azure Relay at all."
    });
});

int localApiPort = builder.Configuration.GetValue("LocalApi:Port", 5080);
builder.WebHost.UseUrls($"http://localhost:{localApiPort}");

var app = builder.Build();

// Kestrel here only binds to localhost for docs/testing, so Swagger is left
// on unconditionally rather than gated behind an environment check.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

Console.WriteLine("=== ProdHelperService ===");
Console.WriteLine($"Swagger UI : http://localhost:{localApiPort}/swagger");
Console.WriteLine($"Relay      : {builder.Configuration["Relay:Namespace"]}/{builder.Configuration["Relay:ConnectionName"]}");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

app.Run();
