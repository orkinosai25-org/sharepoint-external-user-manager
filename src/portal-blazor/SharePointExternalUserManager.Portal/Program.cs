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

// Check for critical errors - fail fast if required settings are missing
if (!validationResult.IsValid)
{
    logger.LogError("═══════════════════════════════════════════════════════════════");
    logger.LogError("CONFIGURATION ERROR: Required settings are missing");
    logger.LogError("═══════════════════════════════════════════════════════════════");
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
    logger.LogError("  2. Navigate to Settings → Environment variables");
    logger.LogError("  3. Add the following Application Settings:");
    logger.LogError("     • AzureAd__ClientSecret = [Your Client Secret]");
    logger.LogError("  4. Click Save and Restart the App Service");
    logger.LogError("");
    logger.LogError("To get the ClientSecret from Azure AD:");
    logger.LogError("  1. Go to Azure Portal → Azure Active Directory");
    logger.LogError("  2. Navigate to App registrations → Find your application");
    logger.LogError("  3. Go to Certificates & secrets");
    logger.LogError("  4. Create a new client secret");
    logger.LogError("  5. Copy the secret value immediately");
    logger.LogError("  6. Add it to App Service configuration");
    logger.LogError("");
    logger.LogError("For local development:");
    logger.LogError("  Option 1: Create appsettings.Local.json (recommended)");
    logger.LogError("    Copy appsettings.example.json and update with your credentials");
    logger.LogError("");
    logger.LogError("  Option 2: Use User Secrets (recommended)");
    logger.LogError("    dotnet user-secrets set \"AzureAd:ClientSecret\" \"YOUR_SECRET\"");
    logger.LogError("");
    logger.LogError("  Option 3: Use Environment Variables");
    logger.LogError("    export AzureAd__ClientSecret=\"YOUR_SECRET\"");
    logger.LogError("");
    logger.LogError("See TROUBLESHOOTING_AADSTS7000218.md for detailed instructions");
    logger.LogError("═══════════════════════════════════════════════════════════════");
    
    throw new InvalidOperationException(
        "Application cannot start due to missing required configuration. " +
        "See logs above for details and instructions on how to fix the configuration.");
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

        // Explicitly set ClientSecret from configuration to ensure it's properly passed to Azure AD
        // This fixes AADSTS7000218 error where client_secret is required but not included in token request
        var clientSecret = builder.Configuration["AzureAd:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            options.ClientSecret = clientSecret;
        }

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
