using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for Azure AD admin consent flow
/// </summary>
[ApiController]
[Route("[controller]")]
public class ConsentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConsentController> _logger;
    private readonly IConfiguration _configuration;

    public ConsentController(
        ApplicationDbContext context,
        ILogger<ConsentController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initiates the Azure AD admin consent flow
    /// Returns the consent URL that admins should visit to grant permissions
    /// </summary>
    /// <param name="redirectUri">Optional redirect URI after consent is granted</param>
    /// <returns>Consent URL and instructions</returns>
    [HttpGet("url")]
    public IActionResult GetConsentUrl([FromQuery] string? redirectUri)
    {
        var clientId = _configuration["AzureAd:ClientId"];
        var tenantId = _configuration["AzureAd:TenantId"] ?? "common";

        if (string.IsNullOrEmpty(clientId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "CONFIG_ERROR",
                "Azure AD Client ID is not configured"));
        }

        // Default redirect URI if not provided
        var effectiveRedirectUri = redirectUri ?? 
            $"{Request.Scheme}://{Request.Host}/consent/callback";

        // Construct the admin consent URL
        var consentUrl = $"https://login.microsoftonline.com/{tenantId}/v2.0/adminconsent" +
            $"?client_id={clientId}" +
            $"&redirect_uri={Uri.EscapeDataString(effectiveRedirectUri)}" +
            $"&scope=https://graph.microsoft.com/.default";

        var response = new
        {
            consentUrl,
            instructions = new[]
            {
                "Have a Global Administrator or Application Administrator visit the consent URL",
                "Review the requested permissions",
                "Grant consent for the organization",
                "You will be redirected back to complete the onboarding"
            },
            requiredPermissions = new[]
            {
                "Sites.ReadWrite.All - Create and manage SharePoint sites",
                "User.ReadWrite.All - Invite external users",
                "Directory.Read.All - Read directory data"
            },
            redirectUri = effectiveRedirectUri
        };

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    /// <summary>
    /// Callback endpoint after admin consent is granted
    /// This endpoint receives the callback from Azure AD
    /// </summary>
    /// <param name="admin_consent">Whether consent was granted</param>
    /// <param name="tenant">Tenant ID that granted consent</param>
    /// <param name="error">Error code if consent was denied</param>
    /// <param name="error_description">Error description</param>
    [HttpGet("callback")]
    public async Task<IActionResult> ConsentCallback(
        [FromQuery] string? admin_consent,
        [FromQuery] string? tenant,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning(
                "Admin consent denied or error occurred. Error: {Error}, Description: {Description}",
                error,
                error_description);

            return BadRequest(ApiResponse<object>.ErrorResponse(
                error ?? "CONSENT_ERROR",
                error_description ?? "Admin consent was not granted"));
        }

        if (admin_consent != "True" || string.IsNullOrEmpty(tenant))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "INVALID_CONSENT",
                "Invalid consent response from Azure AD"));
        }

        _logger.LogInformation(
            "Admin consent granted for tenant {TenantId}",
            tenant);

        // Check if tenant already exists
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenant);

        if (existingTenant != null)
        {
            // Update tenant to mark consent as granted
            existingTenant.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Consent recorded for existing tenant {TenantId}",
                tenant);
        }

        var response = new
        {
            success = true,
            tenantId = tenant,
            message = "Admin consent granted successfully",
            nextSteps = new[]
            {
                "Complete tenant registration at /tenants/register",
                "Configure your organization settings",
                "Start creating client spaces"
            }
        };

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    /// <summary>
    /// Check the consent status for a tenant
    /// Requires authentication
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetConsentStatus()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant claim"));
        }

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        var response = new
        {
            tenantId = tenantIdClaim,
            isRegistered = tenant != null,
            consentGranted = tenant != null,
            status = tenant?.Status ?? "Not Registered",
            requiresAction = tenant == null
        };

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }
}
