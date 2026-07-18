using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProdHelperService;
using ProdHelperService.Auth;
using ProdHelperService.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "RELAY_");

builder.Services.AddProdHelperControllers();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<RelayListenerHostedService>();

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
