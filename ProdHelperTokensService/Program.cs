using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProdHelperTokensService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TokensDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProdHelpTokensDb")));

builder.Services.AddScoped<ApiKeyAuthFilter>();
builder.Services.AddControllers(options => options.Filters.Add<ApiKeyAuthFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProdHelperTokensService API",
        Version = "v1",
        Description = "Central usage-tracking API. Every customer's on-prem ProdHelperService " +
                      "install reports its usage here every few minutes, authenticated with a " +
                      "per-customer API key (X-Api-Key header).",
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Per-customer API key.",
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("ApiKey", doc), [] }
    });
});

int localApiPort = builder.Configuration.GetValue("LocalApi:Port", 5090);
builder.WebHost.UseUrls($"http://localhost:{localApiPort}");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

Console.WriteLine("=== ProdHelperTokensService ===");
Console.WriteLine($"Swagger UI : http://localhost:{localApiPort}/swagger");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

app.Run();
