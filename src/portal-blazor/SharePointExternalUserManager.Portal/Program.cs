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

// Fail application startup if critical configuration is missing
// This prevents authentication errors at runtime
if (!validationResult.IsValid)
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    
    logger.LogError("═══════════════════════════════════════════════════════════════");
    logger.LogError("CONFIGURATION ERROR: Required settings are missing");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    logger.LogError("");
    logger.LogError("The following configuration errors were found:");
    logger.LogError("");
    
    foreach (var error in validationResult.Errors)
    {
        logger.LogError("  • {Key}: {Message}", error.Key, error.Message);
    }
    
    logger.LogError("");
    logger.LogError("APPLICATION CANNOT START without these required settings.");
    logger.LogError("");
    logger.LogError("HOW TO FIX:");
    logger.LogError("");
    logger.LogError("For Azure App Service deployments:");
    logger.LogError("  1. Go to Azure Portal → Your App Service");
    logger.LogError("  2. Navigate to Settings → Environment variables (or Configuration)");
    logger.LogError("  3. Add the following Application Settings:");
    logger.LogError("     • ASPNETCORE_ENVIRONMENT = Production (to use appsettings.Production.json)");
    logger.LogError("     • AzureAd__ClientId = Your Azure AD Application Client ID");
    logger.LogError("     • AzureAd__ClientSecret = Your Azure AD Application Client Secret");
    logger.LogError("     • AzureAd__TenantId = Your Azure AD Tenant ID");
    logger.LogError("     • ApiSettings__BaseUrl = Your backend API URL");
    logger.LogError("  4. Restart the App Service");
    logger.LogError("");
    logger.LogError("  NOTE: If using GitHub Actions secrets, ensure ASPNETCORE_ENVIRONMENT=Production");
    logger.LogError("        is set in Azure App Service to use appsettings.Production.json");
    logger.LogError("");
    logger.LogError("For local development:");
    logger.LogError("  1. Use user secrets (recommended):");
    logger.LogError("     dotnet user-secrets set \"AzureAd:ClientId\" \"YOUR_CLIENT_ID\"");
    logger.LogError("     dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
    logger.LogError("     dotnet user-secrets set \"AzureAd:TenantId\" \"YOUR_TENANT_ID\"");
    logger.LogError("");
    logger.LogError("  2. Or set environment variables:");
    logger.LogError("     export AzureAd__ClientId=\"YOUR_CLIENT_ID\"");
    logger.LogError("     export AzureAd__ClientSecret=\"YOUR_SECRET\"");
    logger.LogError("     export AzureAd__TenantId=\"YOUR_TENANT_ID\"");
    logger.LogError("");
    logger.LogError("For more information:");
    logger.LogError("  • AZURE_APP_SERVICE_CONFIGURATION.md - Azure App Service setup");
    logger.LogError("  • CONFIGURATION_GUIDE.md - General configuration guide");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    
    throw new InvalidOperationException(
        "Application configuration is invalid. Required Azure AD settings (ClientId, ClientSecret, TenantId) are missing. " +
        "Please configure these settings in Azure App Service Environment variables or user secrets. " +
        "See the error log above for detailed instructions.");
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
