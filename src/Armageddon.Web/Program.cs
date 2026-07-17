using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Armageddon.Web.Components;
using Armageddon.Web.Components.Account;
using Armageddon.Web.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var httpPort  = builder.Configuration.GetValue<int?>("App:HttpPort")  ?? 8080;
var httpsPort = builder.Configuration.GetValue<int?>("App:HttpsPort") ?? 8443;
// The public HTTPS port is the port the browser connects to (i.e. the host-side of the
// Docker port mapping). It may differ from the internal Kestrel httpsPort when running
// behind a port-mapped container (e.g. host:8444 -> container:8443).
// If not configured it falls back to the internal Kestrel port (works for local dev).
var publicHttpsPort = builder.Configuration.GetValue<int?>("PublicHttpsPort") ?? httpsPort;
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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = "BlazorChallenge";
})
.AddCookie("BlazorChallenge", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});
builder.Services.AddAuthorization();

// The Web app no longer hosts any Identity DbContexts or local database files. Authentication is handled by the API via JWT.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
// Replace the Identity revalidating provider with a simple AuthenticationStateProvider that uses API-issued JWT.
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
// Register the concrete provider also as the IApiAuthenticationStateProvider abstraction for AuthService
builder.Services.AddScoped<IApiAuthenticationStateProvider>(sp => (IApiAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

// Web app no longer hosts Identity database; ApplicationDbContext moved to API. No DbContext or local DB files should exist in the Web project.

// Identity and user store are owned by the API now. The Web app uses API-issued JWTs.

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8081";

// Persist DataProtection keys to the mounted volume so they survive container restarts.
// Without this the antiforgery tokens encrypted by one container instance cannot be
// decrypted after a restart (key ring is regenerated in-memory each time).
var dataKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/data/dp-keys";
builder.Services.AddDataProtection()
    .SetApplicationName("Armageddon.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(dataKeysPath));

// Shared token store so all HttpClient instances carry the same Bearer token
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddTransient<AuthTokenHandler>();

// Per-session game configuration (timer duration, etc.)
builder.Services.AddScoped<GameSettingsService>();

// The API uses a self-signed certificate. In production inside Docker the Web container
// calls the API over https://api:8443, so we must bypass standard CA validation and
// instead pin to the known certificate thumbprint configured via ApiCertThumbprint.
var apiCertThumbprint = builder.Configuration["ApiCertThumbprint"] ?? string.Empty;

builder.Services.AddHttpClient<AuthService>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = string.IsNullOrEmpty(apiCertThumbprint)
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : (_, cert, _, _) => cert?.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA1)
                    ?.Equals(apiCertThumbprint, StringComparison.OrdinalIgnoreCase) == true
    });

builder.Services.AddHttpClient<IArmageddonApiClient, ArmageddonApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = string.IsNullOrEmpty(apiCertThumbprint)
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : (_, cert, _, _) => cert?.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA1)
                    ?.Equals(apiCertThumbprint, StringComparison.OrdinalIgnoreCase) == true
    });



// Configure HTTPS redirection to use the public-facing port (host port mapping).
// Only registered when a cert is configured so the integration test host (HTTP-only) is unaffected.
if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
{
    builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));
    builder.Services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(options =>
        options.HttpsPort = publicHttpsPort);
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
}
// Use redirects for status code pages to avoid re-executing component endpoints
// which can trigger interactive server component antiforgery validation on a GET (no Content-Type).
app.UseStatusCodePagesWithRedirects("/not-found");

// Sanitize any malformed Content-Type headers (some clients may send an empty header value)
// to avoid FormFeature throwing when attempting to read form data for component endpoints.
// If the header exists but is empty, set a safe default for form parsing on verb methods that may include form data.
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("Content-Type", out var ct))
    {
        var ctValue = ct.ToString();
        if (string.IsNullOrWhiteSpace(ctValue))
        {
            // If this is a method that might include form data, provide a conservative default Content-Type
            // so the framework's form reader does not throw for an empty header value.
            if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
            {
                context.Request.Headers["Content-Type"] = "application/x-www-form-urlencoded; charset=UTF-8";
            }
            else
            {
                // For other methods, remove the header entirely to avoid confusing downstream readers
                context.Request.Headers.Remove("Content-Type");
            }
        }
    }
    await next();
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Lightweight health endpoint to verify the Web app is running
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Identity endpoints are provided by the API; no local Identity endpoints to map.

app.Run();

namespace Armageddon.Web
{
    public partial class Program { }
}
