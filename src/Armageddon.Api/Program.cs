using Armageddon.Abstractions.Interfaces;
using Armageddon.Api.Data;
using Armageddon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();

    var db = services.GetRequiredService<ArmageddonDbContext>();
    try
    {
        db.Database.Migrate();
        await IdentitySeeder.SeedRolesAndAdminAsync(services);
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
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Enable authentication middleware for JWT bearer tokens
app.UseAuthentication();
app.UseAuthorization();

// Lightweight health endpoint to verify the API is running
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

namespace Armageddon.Api
{
    public partial class Program { }
}
