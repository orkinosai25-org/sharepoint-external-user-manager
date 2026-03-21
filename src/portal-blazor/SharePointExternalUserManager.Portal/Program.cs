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
    logger.LogError("NOTE: If you see AADSTS7000215 ('Invalid client secret'), ensure you copy");
    logger.LogError("      the secret VALUE from Azure Portal, not the secret ID (GUID).");
    logger.LogError("═══════════════════════════════════════════════════════════════");

    // Always allow the app to start so that the full OIDC exception is surfaced in the
    // browser (via the developer exception page). Sign-in will fail at runtime with the
    // detailed Azure AD error — fix the configuration error shown above to resolve it.
    logger.LogWarning("Application starting despite configuration errors. " +
        "Sign-in will fail at runtime and the full exception will be shown in the browser.");
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

// Add authentication with Microsoft Entra ID.
// Use the configuration-section overload so that Microsoft.Identity.Web reads ALL AzureAd
// settings (including ClientSecret) directly from the configuration system and properly
// wires them into the MSAL confidential-client pipeline.  This is the recommended pattern
// and fixes AADSTS7000218 ("client_secret or client_assertion required") that occurred when
// the action-based overload with manual Bind did not propagate ClientSecret into MSAL.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Use authorization code flow only (more secure, avoids AADSTS700054 about id_token).
// PostConfigure runs after the library's own configuration so this value always wins.
// OnRemoteFailure is intentionally not overridden here: Azure AD authentication errors
// (AADSTS7000215, AADSTS7000218, etc.) propagate unhandled so the ASP.NET Core developer
// exception page shows the full stack trace and error details in the browser.
builder.Services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
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
    var baseUrl = apiSettings?.BaseUrl;
    var timeout = TimeSpan.FromSeconds(apiSettings?.Timeout > 0 ? apiSettings!.Timeout : 30);

    // Only set BaseAddress when we have a valid, non-loopback URL, or when running in
    // Development on a local machine (where localhost is intentional).  Leaving BaseAddress
    // unset when the URL points to a loopback address in a hosted/production environment
    // prevents the confusing socket-access error and lets pages surface a cleaner "not
    // configured" message instead.
    // EnvironmentHelper.IsAzureAppService detects Azure App Service even when
    // ASPNETCORE_ENVIRONMENT is set to "Development" on the App Service.
    if (apiSettings != null && !string.IsNullOrEmpty(baseUrl) &&
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) &&
        (!baseUri.IsLoopback || (builder.Environment.IsDevelopment() && !EnvironmentHelper.IsAzureAppService)))
    {
        client.BaseAddress = baseUri;
    }

    client.Timeout = timeout;
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
// Use the developer exception page in Development only so that the full exception details
// (including AADSTS7000215 / AADSTS7000218 OpenIdConnect errors) are printed in the
// browser during local development.  In other environments a generic error page is used
// to avoid leaking implementation details to end-users.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// Keep HSTS enabled to enforce HTTPS connections.
app.UseHsts();

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
