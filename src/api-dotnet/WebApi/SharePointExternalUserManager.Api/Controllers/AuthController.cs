using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for OAuth authentication and tenant authorization
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOAuthService _oauthService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ApplicationDbContext context,
        IOAuthService oauthService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _oauthService = oauthService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Initiate OAuth admin consent flow
    /// Returns authorization URL that user should visit to grant permissions
    /// </summary>
    /// <param name="request">Connect tenant request with redirect URI</param>
    /// <returns>Authorization URL and state parameter</returns>
    [HttpPost("connect")]
    [Authorize]
    public async Task<IActionResult> ConnectTenant([FromBody] ConnectTenantRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value ?? "unknown";
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value ?? "unknown";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant claim"));
        }

        _logger.LogInformation(
            "Tenant {TenantId} initiating OAuth consent flow",
            tenantIdClaim);

        try
        {
            // Ensure tenant exists in database
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "TENANT_NOT_FOUND",
                    "Tenant must be registered before connecting. Please complete onboarding first."));
            }

            // Create state for CSRF protection
            var state = new OAuthState
            {
                TenantId = tenantIdClaim,
                RedirectUri = request.RedirectUri,
                UserId = userId,
                UserEmail = userEmail,
                CreatedAt = DateTime.UtcNow
            };

            var encodedState = _oauthService.EncodeState(state);

            // Generate authorization URL
            var authUrl = _oauthService.GenerateAuthorizationUrl(request.RedirectUri, encodedState);

            // Log audit event
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "OAUTH_CONNECT_INITIATED",
                "Tenant",
                tenant.Id.ToString(),
                "OAuth consent flow initiated",
                ipAddress,
                correlationId,
                "Success");

            var response = new ConnectTenantResponse
            {
                AuthorizationUrl = authUrl,
                State = encodedState
            };

            return Ok(ApiResponse<ConnectTenantResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating OAuth consent for tenant {TenantId}", tenantIdClaim);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "An error occurred while initiating consent flow"));
        }
    }

    /// <summary>
    /// OAuth callback endpoint - handles redirect from Microsoft after admin consent
    /// </summary>
    /// <param name="code">Authorization code from Microsoft</param>
    /// <param name="state">State parameter for CSRF validation</param>
    /// <param name="admin_consent">Whether admin consent was granted</param>
    /// <param name="tenant">Tenant ID from Microsoft</param>
    /// <param name="error">Error code if consent failed</param>
    /// <param name="error_description">Error description if consent failed</param>
    [HttpGet("callback")]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? admin_consent,
        [FromQuery] string? tenant,
        [FromQuery] string? error,
        [FromQuery] string? error_description)
    {
        var correlationId = Guid.NewGuid().ToString();

        // Handle error case
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning(
                "OAuth consent failed. Error: {Error}, Description: {Description}",
                error,
                error_description);

            // Redirect back to portal with error
            return Redirect($"/onboarding/consent?error={Uri.EscapeDataString(error)}&error_description={Uri.EscapeDataString(error_description ?? "Unknown error")}");
        }

        // Validate required parameters
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            _logger.LogWarning("OAuth callback received with missing parameters");
            return Redirect("/onboarding/consent?error=invalid_request&error_description=Missing required parameters");
        }

        // Decode and validate state
        var oauthState = _oauthService.DecodeState(state);
        if (oauthState == null)
        {
            _logger.LogWarning("Invalid OAuth state parameter");
            return Redirect("/onboarding/consent?error=invalid_state&error_description=Invalid state parameter");
        }

        // Check state expiration (10 minutes)
        if ((DateTime.UtcNow - oauthState.CreatedAt).TotalMinutes > 10)
        {
            _logger.LogWarning("OAuth state has expired");
            return Redirect("/onboarding/consent?error=state_expired&error_description=Authorization session expired. Please try again.");
        }

        try
        {
            // Exchange authorization code for tokens
            var tokenResponse = await _oauthService.ExchangeCodeForTokensAsync(code, oauthState.RedirectUri);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
            {
                _logger.LogError("Failed to exchange code for tokens");
                return Redirect("/onboarding/consent?error=token_exchange_failed&error_description=Failed to obtain access tokens");
            }

            // Get tenant from database
            var tenantEntity = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == oauthState.TenantId);

            if (tenantEntity == null)
            {
                _logger.LogError("Tenant {TenantId} not found in database", oauthState.TenantId);
                return Redirect("/onboarding/consent?error=tenant_not_found&error_description=Tenant not found");
            }

            // Calculate token expiration
            var tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);

            // Store or update tenant auth tokens
            var existingAuth = await _context.TenantAuth
                .FirstOrDefaultAsync(ta => ta.TenantId == tenantEntity.Id);

            if (existingAuth != null)
            {
                // Update existing auth
                existingAuth.AccessToken = tokenResponse.access_token;
                existingAuth.RefreshToken = tokenResponse.refresh_token;
                existingAuth.TokenExpiresAt = tokenExpiresAt;
                existingAuth.Scope = tokenResponse.scope;
                existingAuth.ConsentGrantedBy = oauthState.UserEmail;
                existingAuth.ConsentGrantedAt = DateTime.UtcNow;
                existingAuth.ModifiedDate = DateTime.UtcNow;
            }
            else
            {
                // Create new auth record
                var newAuth = new TenantAuthEntity
                {
                    TenantId = tenantEntity.Id,
                    AccessToken = tokenResponse.access_token,
                    RefreshToken = tokenResponse.refresh_token,
                    TokenExpiresAt = tokenExpiresAt,
                    Scope = tokenResponse.scope,
                    ConsentGrantedBy = oauthState.UserEmail,
                    ConsentGrantedAt = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                _context.TenantAuth.Add(newAuth);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "OAuth tokens stored successfully for tenant {TenantId}",
                oauthState.TenantId);

            // Log audit event
            await _auditLogService.LogActionAsync(
                tenantEntity.Id,
                oauthState.UserId ?? "system",
                oauthState.UserEmail ?? "unknown",
                "TENANT_CONSENT_GRANTED",
                "Tenant",
                tenantEntity.Id.ToString(),
                $"Admin consent granted by {oauthState.UserEmail}",
                "callback",
                correlationId,
                "Success");

            // Redirect back to portal with success
            return Redirect($"{oauthState.RedirectUri}?admin_consent=True&tenant={Uri.EscapeDataString(oauthState.TenantId)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OAuth callback");
            return Redirect("/onboarding/consent?error=callback_error&error_description=An error occurred processing the authorization");
        }
    }

    /// <summary>
    /// Validate Microsoft Graph permissions for current tenant
    /// Checks if all required permissions are granted and tokens are valid
    /// </summary>
    [HttpGet("permissions")]
    [Authorize]
    public async Task<IActionResult> ValidatePermissions()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant claim"));
        }

        try
        {
            // Get tenant from database
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

            if (tenant == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(
                    "TENANT_NOT_FOUND",
                    "Tenant not found"));
            }

            // Get tenant auth
            var tenantAuth = await _context.TenantAuth
                .FirstOrDefaultAsync(ta => ta.TenantId == tenant.Id);

            if (tenantAuth == null || string.IsNullOrEmpty(tenantAuth.AccessToken))
            {
                var response = new ValidatePermissionsResponse
                {
                    HasRequiredPermissions = false,
                    GrantedPermissions = new List<string>(),
                    MissingPermissions = _oauthService.RequiredPermissions,
                    TokenExpired = false,
                    TokenRefreshed = false
                };

                return Ok(ApiResponse<ValidatePermissionsResponse>.SuccessResponse(response));
            }

            var accessToken = tenantAuth.AccessToken;
            var tokenRefreshed = false;

            // Check if token is expired or about to expire (within 5 minutes)
            if (tenantAuth.TokenExpiresAt.HasValue &&
                tenantAuth.TokenExpiresAt.Value < DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogInformation("Access token expired or expiring soon for tenant {TenantId}, attempting refresh", tenantIdClaim);

                // Try to refresh token
                if (!string.IsNullOrEmpty(tenantAuth.RefreshToken))
                {
                    var tokenResponse = await _oauthService.RefreshAccessTokenAsync(tenantAuth.RefreshToken);

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.access_token))
                    {
                        // Update tokens in database
                        tenantAuth.AccessToken = tokenResponse.access_token;
                        tenantAuth.RefreshToken = tokenResponse.refresh_token ?? tenantAuth.RefreshToken;
                        tenantAuth.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);
                        tenantAuth.LastTokenRefresh = DateTime.UtcNow;
                        tenantAuth.ModifiedDate = DateTime.UtcNow;

                        await _context.SaveChangesAsync();

                        accessToken = tokenResponse.access_token;
                        tokenRefreshed = true;

                        _logger.LogInformation("Access token refreshed successfully for tenant {TenantId}", tenantIdClaim);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to refresh access token for tenant {TenantId}", tenantIdClaim);

                        var response = new ValidatePermissionsResponse
                        {
                            HasRequiredPermissions = false,
                            GrantedPermissions = new List<string>(),
                            MissingPermissions = _oauthService.RequiredPermissions,
                            TokenExpired = true,
                            TokenRefreshed = false,
                            ConsentGrantedAt = tenantAuth.ConsentGrantedAt,
                            ConsentGrantedBy = tenantAuth.ConsentGrantedBy
                        };

                        return Ok(ApiResponse<ValidatePermissionsResponse>.SuccessResponse(response));
                    }
                }
            }

            // Validate permissions
            var (hasPermissions, grantedPermissions, missingPermissions) = 
                await _oauthService.ValidatePermissionsAsync(accessToken);

            var validationResponse = new ValidatePermissionsResponse
            {
                HasRequiredPermissions = hasPermissions,
                GrantedPermissions = grantedPermissions,
                MissingPermissions = missingPermissions,
                TokenExpired = false,
                TokenRefreshed = tokenRefreshed,
                ConsentGrantedAt = tenantAuth.ConsentGrantedAt,
                ConsentGrantedBy = tenantAuth.ConsentGrantedBy
            };

            return Ok(ApiResponse<ValidatePermissionsResponse>.SuccessResponse(validationResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permissions for tenant {TenantId}", tenantIdClaim);

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "An error occurred while validating permissions"));
        }
    }
}
