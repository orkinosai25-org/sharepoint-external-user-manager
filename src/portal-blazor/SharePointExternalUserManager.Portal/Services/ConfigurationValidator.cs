using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

/// <summary>
/// Validates application configuration settings to ensure all required values are properly configured
/// </summary>
public class ConfigurationValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator(IConfiguration configuration, ILogger<ConfigurationValidator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Validates all critical configuration settings
    /// </summary>
    /// <returns>Validation result with any errors found</returns>
    public ConfigurationValidationResult Validate()
    {
        var result = new ConfigurationValidationResult();

        // Validate Azure AD configuration
        ValidateAzureAd(result);

        // Validate API settings (warning only, not critical)
        ValidateApiSettings(result);

        // Validate Stripe settings (warning only, not critical for basic functionality)
        ValidateStripeSettings(result);

        return result;
    }

    private void ValidateAzureAd(ConfigurationValidationResult result)
    {
        var azureAd = _configuration.GetSection("AzureAd").Get<AzureAdSettings>();
        
        if (azureAd == null)
        {
            result.AddWarning("AzureAd", "AzureAd configuration section is missing. Authentication cannot work without this configuration.");
            return;
        }

        // Check ClientId - treat as warning if missing or placeholder
        if (IsPlaceholder(azureAd.ClientId))
        {
            result.AddWarning("AzureAd:ClientId", 
                "Azure AD Client ID contains a placeholder value. Please set it via environment variables, user secrets, or appsettings.Local.json.");
            _logger.LogWarning("CONFIGURATION WARNING: Azure AD ClientId contains placeholder value '{ClientId}'. " +
                "Authentication will not work until a valid Client ID is configured.", 
                azureAd.ClientId);
        }
        else if (string.IsNullOrWhiteSpace(azureAd.ClientId))
        {
            result.AddWarning("AzureAd:ClientId", "Azure AD Client ID is required for authentication. Please set via environment variables, user secrets, or appsettings.Local.json.");
        }

        // Check ClientSecret - treat as warning if missing or placeholder
        if (IsPlaceholder(azureAd.ClientSecret))
        {
            result.AddWarning("AzureAd:ClientSecret", 
                "Azure AD Client Secret contains a placeholder value. Please set it via environment variables, user secrets, or appsettings.Local.json.");
            _logger.LogWarning("CONFIGURATION WARNING: Azure AD ClientSecret contains placeholder value. " +
                "Authentication will not work until a valid Client Secret is configured via secure methods.");
        }
        else if (string.IsNullOrWhiteSpace(azureAd.ClientSecret))
        {
            result.AddWarning("AzureAd:ClientSecret", "Azure AD Client Secret is required for authentication. Please set via environment variables, user secrets, or appsettings.Local.json.");
        }

        // Check TenantId - treat as warning if missing
        if (string.IsNullOrWhiteSpace(azureAd.TenantId))
        {
            result.AddWarning("AzureAd:TenantId", "Azure AD Tenant ID is required for authentication. Please set via environment variables, user secrets, or appsettings.Local.json.");
        }
    }

    private void ValidateApiSettings(ConfigurationValidationResult result)
    {
        var apiSettings = _configuration.GetSection("ApiSettings").Get<ApiSettings>();
        
        if (apiSettings == null || string.IsNullOrWhiteSpace(apiSettings.BaseUrl))
        {
            result.AddWarning("ApiSettings:BaseUrl", 
                "Backend API URL is not configured. API functionality may be limited.");
        }
    }

    private void ValidateStripeSettings(ConfigurationValidationResult result)
    {
        var stripeSettings = _configuration.GetSection("StripeSettings").Get<StripeSettings>();
        
        if (stripeSettings == null || IsPlaceholder(stripeSettings.PublishableKey))
        {
            result.AddWarning("StripeSettings:PublishableKey", 
                "Stripe Publishable Key is not configured. Billing functionality will not work.");
        }
    }

    /// <summary>
    /// Checks if a value is a placeholder (contains YOUR_, _HERE, etc.)
    /// </summary>
    private static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var placeholderIndicators = new[] 
        { 
            "YOUR_", 
            "_HERE", 
            "REPLACE_", 
            "PLACEHOLDER",
            "EXAMPLE_",
            "TODO"
        };

        return placeholderIndicators.Any(indicator => 
            value.Contains(indicator, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Result of configuration validation
/// </summary>
public class ConfigurationValidationResult
{
    public List<ConfigurationError> Errors { get; } = new();
    public List<ConfigurationError> Warnings { get; } = new();

    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string key, string message)
    {
        Errors.Add(new ConfigurationError { Key = key, Message = message });
    }

    public void AddWarning(string key, string message)
    {
        Warnings.Add(new ConfigurationError { Key = key, Message = message });
    }

    public string GetErrorSummary()
    {
        if (!Errors.Any())
            return string.Empty;

        return string.Join("\n", Errors.Select(e => $"• {e.Key}: {e.Message}"));
    }
}

/// <summary>
/// Represents a configuration error or warning
/// </summary>
public class ConfigurationError
{
    public string Key { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
