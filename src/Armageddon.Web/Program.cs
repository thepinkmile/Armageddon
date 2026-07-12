using Microsoft.AspNetCore.Components.Authorization;
using Armageddon.Web.Components;
using Armageddon.Web.Components.Account;
using Armageddon.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The Web app no longer hosts any Identity DbContexts or local database files. Authentication is handled by the API via JWT.
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
// Replace the Identity revalidating provider with a simple AuthenticationStateProvider that uses API-issued JWT.
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
// Register the concrete provider also as the IApiAuthenticationStateProvider abstraction for AuthService
builder.Services.AddScoped<IApiAuthenticationStateProvider>(sp => (IApiAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

// Web app no longer hosts Identity database; ApplicationDbContext moved to API. No DbContext or local DB files should exist in the Web project.

// Identity and user store are owned by the API now. The Web app uses API-issued JWTs.

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5001";

// Shared token store so all HttpClient instances carry the same Bearer token
builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddTransient<AuthTokenHandler>();

// Register AuthService to call API for authentication. Use a typed HttpClient with the API base address
// so calls like PostAsJsonAsync("api/auth/login", ...) use an absolute URI.
builder.Services.AddHttpClient<AuthService>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();
builder.Services.AddHttpClient<IArmageddonApiClient, ArmageddonApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthTokenHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
// Use redirects for status code pages to avoid re-executing component endpoints
// which can trigger interactive server component antiforgery validation on a GET (no Content-Type).
app.UseStatusCodePagesWithRedirects("/not-found");
app.UseHttpsRedirection();

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
