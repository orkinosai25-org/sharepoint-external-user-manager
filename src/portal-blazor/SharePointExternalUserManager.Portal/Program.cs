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

// Log configuration warnings but allow application to start
// This allows developers to run the app locally and configure settings as needed
if (!validationResult.IsValid || validationResult.HasWarnings)
{
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("CONFIGURATION WARNING: Some settings are not fully configured");
    logger.LogWarning("═══════════════════════════════════════════════════════════════");
    logger.LogWarning("");
    logger.LogWarning("The application will start, but some features may not work:");
    logger.LogWarning("");
    
    foreach (var warning in validationResult.Warnings)
    {
        logger.LogWarning("  • {Key}: {Message}", warning.Key, warning.Message);
    }
    
    logger.LogWarning("");
    logger.LogWarning("HOW TO CONFIGURE SETTINGS:");
    logger.LogWarning("");
    logger.LogWarning("Option 1: appsettings.Local.json (Recommended for local development)");
    logger.LogWarning("  1. Copy appsettings.example.json to appsettings.Local.json");
    logger.LogWarning("  2. Update the values with your actual Azure AD credentials:");
    logger.LogWarning("     - ClientId: Your Azure AD Application Client ID");
    logger.LogWarning("     - ClientSecret: Your Azure AD Application Client Secret");
    logger.LogWarning("     - TenantId: Your Azure AD Tenant ID");
    logger.LogWarning("     - ApiSettings.BaseUrl: Your backend API URL");
    logger.LogWarning("  3. The file is in .gitignore and won't be committed");
    logger.LogWarning("");
    logger.LogWarning("Option 2: User Secrets (Secure alternative)");
    logger.LogWarning("  dotnet user-secrets set \"AzureAd:ClientId\" \"YOUR_CLIENT_ID\"");
    logger.LogWarning("  dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
    logger.LogWarning("  dotnet user-secrets set \"AzureAd:TenantId\" \"YOUR_TENANT_ID\"");
    logger.LogWarning("");
    logger.LogWarning("Option 3: Environment Variables");
    logger.LogWarning("  export AzureAd__ClientId=\"YOUR_CLIENT_ID\"");
    logger.LogWarning("  export AzureAd__ClientSecret=\"YOUR_SECRET\"");
    logger.LogWarning("  export AzureAd__TenantId=\"YOUR_TENANT_ID\"");
    logger.LogWarning("");
    logger.LogWarning("For Azure App Service deployments:");
    logger.LogWarning("  Use Azure App Service Configuration (Environment Variables)");
    logger.LogWarning("  OR ensure appsettings.Production.json is created during deployment");
    logger.LogWarning("");
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
