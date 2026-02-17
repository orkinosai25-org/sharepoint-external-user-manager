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
/// Controller for tenant management operations
/// </summary>
[ApiController]
[Route("[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<TenantsController> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get authenticated tenant context
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var upn = User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant or user claims"));

        // Get tenant details from database
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        var subscription = tenant?.Subscriptions.FirstOrDefault();

        var tenantInfo = new
        {
            tenantId = tenantIdClaim,
            userId,
            userPrincipalName = upn,
            isActive = tenant?.Status == "Active",
            subscriptionTier = subscription?.Tier ?? "Free",
            organizationName = tenant?.OrganizationName ?? "Unknown"
        };

        return Ok(ApiResponse<object>.SuccessResponse(tenantInfo));
    }

    /// <summary>
    /// Register/onboard a new tenant
    /// This endpoint requires authentication but creates tenant if not exists
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] TenantRegistrationRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value ?? request.PrimaryAdminEmail;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        _logger.LogInformation("Tenant registration request for Entra ID Tenant {TenantId}", tenantIdClaim);

        // Check if tenant already exists
        var existingTenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (existingTenant != null)
        {
            _logger.LogWarning("Tenant {TenantId} already registered", tenantIdClaim);
            return Conflict(ApiResponse<object>.ErrorResponse(
                "TENANT_ALREADY_EXISTS",
                "This tenant is already registered. Use GET /tenants/me to retrieve tenant information."));
        }

        // Validate request
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "VALIDATION_ERROR",
                "Organization name is required"));
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryAdminEmail))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "VALIDATION_ERROR",
                "Primary admin email is required"));
        }

        try
        {
            // Create new tenant
            var tenant = new TenantEntity
            {
                EntraIdTenantId = tenantIdClaim,
                OrganizationName = request.OrganizationName,
                PrimaryAdminEmail = request.PrimaryAdminEmail,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                OnboardedDate = DateTime.UtcNow,
                Settings = System.Text.Json.JsonSerializer.Serialize(request.Settings ?? new TenantSettings())
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Create default subscription (Free tier with 30-day trial)
            var subscription = new SubscriptionEntity
            {
                TenantId = tenant.Id,
                Tier = "Free",
                Status = "Trial",
                StartDate = DateTime.UtcNow,
                EndDate = null, // Free tier has no end date
                TrialExpiry = DateTime.UtcNow.AddDays(30), // 30-day trial
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tenant {TenantId} registered successfully with ID {InternalId}",
                tenantIdClaim,
                tenant.Id);

            // Log audit event
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId ?? "system",
                userEmail,
                "TENANT_REGISTERED",
                "Tenant",
                tenant.Id.ToString(),
                $"Tenant registered: {request.OrganizationName}",
                ipAddress,
                correlationId,
                "Success");

            var response = new TenantRegistrationResponse
            {
                TenantId = tenant.Id,
                EntraIdTenantId = tenant.EntraIdTenantId,
                OrganizationName = tenant.OrganizationName,
                SubscriptionTier = subscription.Tier,
                TrialExpiryDate = subscription.TrialExpiry,
                RegisteredDate = tenant.CreatedDate
            };

            return CreatedAtAction(nameof(GetMe), null, ApiResponse<TenantRegistrationResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering tenant {TenantId}", tenantIdClaim);

            // Log failure audit event
            await _auditLogService.LogActionAsync(
                0, // No tenant ID yet
                userId ?? "system",
                userEmail,
                "TENANT_REGISTRATION_FAILED",
                "Tenant",
                tenantIdClaim,
                $"Tenant registration failed: {ex.Message}",
                ipAddress,
                correlationId,
                "Failed");

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "An error occurred while registering the tenant. Please try again later."));
        }
    }
}
