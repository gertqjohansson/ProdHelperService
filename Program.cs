using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using ProdHelperService;
using ProdHelperService.ActionLogging;
using ProdHelperService.Auth;
using ProdHelperService.Controllers;
using ProdHelperService.Contracts.Auth;
using ProdHelperService.ErrorLogging;
using ProdHelperService.ServiceManagement;
using ProdHelperService.Storage;
using ProdHelperService.Translation;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "RELAY_");

// No-op unless actually launched by the Service Control Manager (i.e. after
// WindowsServiceInstaller registers this process as a service) - safe to
// call unconditionally, so `dotnet run`/console usage is unaffected.
builder.Host.UseWindowsService();

builder.Services.AddProdHelperControllers();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<RelayListenerHostedService>();
builder.Services.AddSingleton<IServiceLifecycleManager, ServiceLifecycleManager>();
builder.Services.AddSingleton<IWindowsServiceInstaller, WindowsServiceInstaller>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProdHelperDb")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; // no email sender wired up yet
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Shorter than Identity's 1-day default, consistent with this app's other
// short-lived tokens (5-min MFA challenge, 15-min access token).
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(30));

builder.Services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<TranslationSettings>(builder.Configuration.GetSection("Translation"));
builder.Services.AddHttpClient<LibreTranslateService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<TranslationSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});
builder.Services.AddHttpClient<MyMemoryTranslationService>((sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<TranslationSettings>>().Value;
    client.BaseAddress = new Uri(settings.MyMemoryBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});
builder.Services.AddTransient<ITranslationService, CompositeTranslationService>();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddTransient<IFileStorageService, FileStorageService>();

builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<IActionLogService, ActionLogService>();

builder.Services.AddMemoryCache();
builder.Services.AddProdHelperAuth(builder.Configuration);
builder.Services.AddAuthorization();

// The web client runs in a browser on a different origin/port than this API
// (e.g. Vite dev server on :5173), so it needs an explicit CORS policy. The
// AdminApp's plain HttpClient is unaffected — CORS is a browser-only mechanism.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token from Auth/Login or Auth/VerifyMfa (Swagger adds the 'Bearer ' prefix)."
    });
    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), [] }
    });
});

int localApiPort = builder.Configuration.GetValue("LocalApi:Port", 5080);
builder.WebHost.UseUrls($"http://localhost:{localApiPort}");

var app = builder.Build();

// Catches any unhandled exception from any controller, logs it to the ErrorLog table (Section is
// the request path, which every controller in this app already names "{Controller}/{Function}" -
// see ApiRoutes.cs), and returns the same AuthErrorResponse{Code,Message} shape every other error
// response already uses. Deliberate BadRequest/NotFound returns from controllers are normal
// IActionResult values, not exceptions, so this has no effect on them. Registered first so it
// wraps every other middleware below.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is { } exception)
        {
            string section = feature.Path.Trim('/');
            Console.WriteLine($"[Error] {section}: {exception}");
            var errorLogService = context.RequestServices.GetRequiredService<IErrorLogService>();
            await errorLogService.LogAsync(section, exception, context.RequestAborted);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new AuthErrorResponse
        {
            Code = "InternalError",
            Message = "An unexpected error occurred.",
        }));
    });
});

// Kestrel here only binds to localhost for docs/testing, so Swagger is left
// on unconditionally rather than gated behind an environment check.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

Console.WriteLine("=== ProdHelperService ===");
Console.WriteLine($"Swagger UI : http://localhost:{localApiPort}/swagger");
Console.WriteLine($"Relay      : {builder.Configuration["Relay:Namespace"]}/{builder.Configuration["Relay:ConnectionName"]}");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

app.Run();
