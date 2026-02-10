using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SharePointExternalUserManager.Portal.Components;
using SharePointExternalUserManager.Portal.Models;
using SharePointExternalUserManager.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Validate configuration early to provide helpful error messages
var configValidator = new ConfigurationValidator(
    builder.Configuration, 
    LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ConfigurationValidator>());

var validationResult = configValidator.Validate();

if (!validationResult.IsValid)
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    logger.LogError("CONFIGURATION ERROR: Application cannot start");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    logger.LogError("");
    logger.LogError("The following configuration errors were found:");
    logger.LogError("");
    
    foreach (var error in validationResult.Errors)
    {
        logger.LogError("  • {Key}: {Message}", error.Key, error.Message);
    }
    
    logger.LogError("");
    logger.LogError("How to fix:");
    logger.LogError("  1. Register an application in Azure Portal:");
    logger.LogError("     https://portal.azure.com → Azure Active Directory → App registrations");
    logger.LogError("");
    logger.LogError("  2. Configure the application using one of these methods:");
    logger.LogError("");
    logger.LogError("     Option A - User Secrets (recommended for development):");
    logger.LogError("       dotnet user-secrets set \"AzureAd:ClientId\" \"YOUR_CLIENT_ID\"");
    logger.LogError("       dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
    logger.LogError("");
    logger.LogError("     Option B - Environment Variables:");
    logger.LogError("       export AzureAd__ClientId=\"YOUR_CLIENT_ID\"");
    logger.LogError("       export AzureAd__ClientSecret=\"YOUR_SECRET\"");
    logger.LogError("");
    logger.LogError("     Option C - appsettings.Development.json (not recommended):");
    logger.LogError("       Update appsettings.Development.json with actual values");
    logger.LogError("       WARNING: Do not commit secrets to source control!");
    logger.LogError("");
    logger.LogError("  3. See QUICKSTART.md for detailed setup instructions");
    logger.LogError("");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    
    throw new InvalidOperationException(
        "Application configuration is invalid. Please configure Azure AD credentials. " +
        "See console output above for details.");
}

if (validationResult.HasWarnings)
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    logger.LogWarning("Configuration warnings:");
    foreach (var warning in validationResult.Warnings)
    {
        logger.LogWarning("  • {Key}: {Message}", warning.Key, warning.Message);
    }
}

// Add authentication with Microsoft Entra ID
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add authorization
builder.Services.AddAuthorization();

// Configure API settings
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

// Add HttpClient for API calls
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>();
    if (apiSettings != null && !string.IsNullOrEmpty(apiSettings.BaseUrl))
    {
        client.BaseAddress = new Uri(apiSettings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(apiSettings.Timeout);
    }
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();
app.MapRazorPages();

app.Run();
