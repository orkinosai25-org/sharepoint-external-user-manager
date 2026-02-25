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

// Add appsettings.Local.json to configuration sources
// This file is in .gitignore and can contain local development secrets
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Validate configuration early to provide helpful error messages
var configValidator = new ConfigurationValidator(
    builder.Configuration, 
    LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ConfigurationValidator>());

var validationResult = configValidator.Validate();

// Create logger for configuration errors
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");

// Check for critical errors - show warnings but allow app to start
if (!validationResult.IsValid)
{
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("CONFIGURATION WARNING: Some required settings are missing");
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("");
    logger.LogWarning("The application will start, but authentication may not work:");
    logger.LogWarning("");
    
    foreach (var error in validationResult.Errors)
    {
        logger.LogWarning("  • {Key}: {Message}", error.Key, error.Message);
    }
    
    logger.LogWarning("");
    logger.LogWarning("HOW TO FIX:");
    logger.LogWarning("");
    logger.LogWarning("For production deployments:");
    logger.LogWarning("  1. Update appsettings.json with your Azure AD credentials");
    logger.LogWarning("  2. Or use Azure App Service Environment variables:");
    logger.LogWarning("     • AzureAd__ClientId = Your Azure AD Application Client ID");
    logger.LogWarning("     • AzureAd__ClientSecret = Your Azure AD Application Client Secret");
    logger.LogWarning("     • AzureAd__TenantId = Your Azure AD Tenant ID");
    logger.LogWarning("");
    logger.LogWarning("For local development:");
    logger.LogWarning("  Option 1: Edit appsettings.json directly (not recommended for secrets)");
    logger.LogWarning("    Add ClientSecret value to AzureAd section");
    logger.LogWarning("");
    logger.LogWarning("  Option 2: Create appsettings.Local.json (recommended)");
    logger.LogWarning("    Copy appsettings.example.json and update with your credentials");
    logger.LogWarning("");
    logger.LogWarning("  Option 3: Use User Secrets");
    logger.LogWarning("    dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
    logger.LogWarning("");
    logger.LogWarning("  Option 4: Use Environment Variables");
    logger.LogWarning("    export AzureAd__ClientSecret=\"YOUR_SECRET\"");
    logger.LogWarning("");
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
}

// Log warnings for non-critical settings
if (validationResult.HasWarnings)
{
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("CONFIGURATION WARNING: Some optional settings are not configured");
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("");
    logger.LogWarning("The application will start, but some features may not work:");
    logger.LogWarning("");
    
    foreach (var warning in validationResult.Warnings)
    {
        logger.LogWarning("  • {Key}: {Message}", warning.Key, warning.Message);
    }
    
    logger.LogWarning("");
    logger.LogWarning("These settings are optional and can be configured later.");
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
}

// Add authentication with Microsoft Entra ID
// Always use configuration from appsettings.json for MVP
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        // Use authorization code flow only (more secure and doesn't require implicit grant)
        // This prevents the AADSTS700054 error about 'id_token' not being enabled
        options.ResponseType = "code";
    });

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

// Add Notification Service
builder.Services.AddScoped<NotificationService>();

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
