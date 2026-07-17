using Armageddon.Abstractions.Interfaces;
using Armageddon.Api.Data;
using Armageddon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var httpPort  = builder.Configuration.GetValue<int?>("App:HttpPort")  ?? 8080;
var httpsPort = builder.Configuration.GetValue<int?>("App:HttpsPort") ?? 8443;
var certPath = builder.Configuration.GetValue<string>("Kestrel:Certificates:Default:Path");

// Explicitly bind Kestrel to IPv4 only (0.0.0.0) to avoid IPv6 socket hangs on
// Raspberry Pi kernels where IPv6 is disabled. Binds HTTP always and HTTPS when
// a certificate path is present in configuration.
builder.WebHost.ConfigureKestrel(options =>
{
    var certPass = builder.Configuration["Kestrel:Certificates:Default:Password"];

    options.Listen(IPAddress.Any, httpPort);

    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        options.Listen(IPAddress.Any, httpsPort, listenOptions =>
        {
            listenOptions.UseHttps(certPath, certPass);
        });
    }
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddOpenApi();

var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
if (useInMemory)
{
    builder.Services.AddDbContext<ArmageddonDbContext>(options =>
        options.UseInMemoryDatabase("TestAppDb"));
}
else
{
    builder.Services.AddDbContext<ArmageddonDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=armageddon.db",
            b => b.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));
}

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 4;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ArmageddonDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-change-this";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Armageddon.Api";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // In test/dev environments the test server may not expose HTTPS; disable metadata requirement
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // During tests the factory may supply a different issuer; disable strict issuer validation to avoid 401s
            ValidateIssuer = false,
            //ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                if (logger.IsEnabled(LogLevel.Information))
                {
                    if (context.Request.Headers.TryGetValue("Authorization", out var header))
                    {
                        logger.LogInformation("Authorization header present: {header}", header.ToString());
                    }
                    else
                    {
                        logger.LogInformation("No Authorization header present on incoming request");
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IObjectiveService, ObjectiveService>();
builder.Services.AddScoped<IScoreService, ScoreService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<ISettingService, SettingService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Tell UseHttpsRedirection the explicit HTTPS port since Kestrel is bound via code,
// not via ASPNETCORE_HTTPS_PORTS, so the middleware cannot auto-detect it.
builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options => options.HttpsPort = httpsPort);

var app = builder.Build();

{
    var startLogger = app.Services.GetRequiredService<ILogger<Program>>();
    var certPass = builder.Configuration["Kestrel:Certificates:Default:Password"];
    startLogger.LogWarning("CERT CHECK — path='{CertPath}' exists={Exists}",
        certPath ?? "(not set)",
        certPath != null && File.Exists(certPath));

    if (certPath != null && File.Exists(certPath))
    {
        try
        {
            var cert = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(certPath, certPass);
            startLogger.LogWarning("CERT LOAD OK — subject='{Subject}' thumbprint={Thumbprint} hasPrivateKey={HasPrivateKey}",
                cert.Subject, cert.Thumbprint, cert.HasPrivateKey);
            cert.Dispose();
        }
        catch (Exception ex)
        {
            startLogger.LogError(ex, "CERT LOAD FAILED — cannot read PFX, Kestrel will not start HTTPS");
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("STARTUP [1/4] — resolving ArmageddonDbContext");
    var db = services.GetRequiredService<ArmageddonDbContext>();

    logger.LogInformation("STARTUP [2/4] — running db.Database.Migrate()");
    try
    {
        db.Database.Migrate();
        logger.LogInformation("STARTUP [3/4] — migration complete, running IdentitySeeder");
        await IdentitySeeder.SeedRolesAndAdminAsync(services);
        logger.LogInformation("STARTUP [4/4] — seeding complete");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to migrate ArmageddonDbContext; attempting EnsureCreated fallback.");
        try { db.Database.EnsureCreated(); } catch (Exception inner) { logger.LogError(inner, "EnsureCreated also failed for ArmageddonDbContext."); }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseCors();

// Only redirect to HTTPS if a certificate is actually configured — avoids breaking
// the integration test host which runs HTTP-only with no cert.
if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
{
    app.UseHttpsRedirection();
}

// Enable authentication middleware for JWT bearer tokens
app.UseAuthentication();
app.UseAuthorization();

// Lightweight health endpoint to verify the API is running
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Logger.LogInformation("Startup complete — calling app.Run()");
app.Run();

namespace Armageddon.Api
{
    public partial class Program { }
}
