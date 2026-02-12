using Microsoft.AspNetCore.Authentication.Cookies;
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

// Show warnings for missing configuration but don't fail the app
// This allows the app to start even with incomplete configuration
if (!validationResult.IsValid)
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    
    // Always show warnings, never fail
    {
        logger.LogWarning("═══════════════════════════════════════════════════════════════");
        logger.LogWarning("CONFIGURATION WARNING: Some settings are not configured");
        logger.LogWarning("═══════════════════════════════════════════════════════════════");
        logger.LogWarning("");
        logger.LogWarning("The following configuration issues were found:");
        logger.LogWarning("");
        
        foreach (var error in validationResult.Errors)
        {
            logger.LogWarning("  • {Key}: {Message}", error.Key, error.Message);
        }
        
        logger.LogWarning("");
        logger.LogWarning("The application will start, but some features may not work.");
        logger.LogWarning("");
        logger.LogWarning("How to fix:");
        logger.LogWarning("  1. Configure application settings via Azure App Service Configuration or environment variables");
        logger.LogWarning("  2. Set the following required settings:");
        logger.LogWarning("     - AzureAd__ClientId: Your Azure AD Application Client ID");
        logger.LogWarning("     - AzureAd__ClientSecret: Your Azure AD Application Client Secret");
        logger.LogWarning("     - AzureAd__TenantId: Your Azure AD Tenant ID");
        logger.LogWarning("     - ApiSettings__BaseUrl: Your backend API URL");
        logger.LogWarning("     - StripeSettings__PublishableKey: Your Stripe Publishable Key (optional)");
        logger.LogWarning("");
        logger.LogWarning("  3. For development, use user secrets:");
        logger.LogWarning("     dotnet user-secrets set \"AzureAd:ClientId\" \"YOUR_CLIENT_ID\"");
        logger.LogWarning("     dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
        logger.LogWarning("");
        logger.LogWarning("═══════════════════════════════════════════════════════════════");
    }
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
// Always use configuration from appsettings.json for MVP
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add authorization
builder.Services.AddAuthorization();

// Configure API settings
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
builder.Services.Configure<AzureOpenAISettings>(builder.Configuration.GetSection("AzureOpenAI"));

// Add HttpClient for API calls
builder.Services.AddHttpClient<ApiClient>(client =>
{
    var apiSettings = builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>();
    if (apiSettings != null && !string.IsNullOrEmpty(apiSettings.BaseUrl) && 
        Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
        client.Timeout = TimeSpan.FromSeconds(apiSettings.Timeout > 0 ? apiSettings.Timeout : 30);
    }
    else
    {
        // Set a default timeout even if BaseUrl is not configured
        client.Timeout = TimeSpan.FromSeconds(30);
    }
});

// Add HttpClient for ChatService
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddScoped<ChatService>();

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
