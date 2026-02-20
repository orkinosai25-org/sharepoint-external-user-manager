using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Authorization;

/// <summary>
/// Authorization handler that validates user roles against tenant-specific role assignments
/// </summary>
public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RoleAuthorizationHandler> _logger;

    public RoleAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext context,
        ILogger<RoleAuthorizationHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogWarning("HttpContext is null in RoleAuthorizationHandler");
            return;
        }

        // Extract user and tenant information from JWT claims
        var userId = context.User.FindFirst("oid")?.Value;
        var tenantIdClaim = context.User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(tenantIdClaim))
        {
            _logger.LogWarning("Missing oid or tid claim in JWT token");
            return;
        }

        // Get tenant ID from database
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for EntraIdTenantId: {TenantId}", tenantIdClaim);
            return;
        }

        // Check if user is the primary admin (automatic TenantOwner)
        if (tenant.PrimaryAdminEmail.Equals(context.User.FindFirst("email")?.Value, 
            StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("User {UserId} is primary admin for tenant {TenantId}", 
                userId, tenant.Id);
            context.Succeed(requirement);
            return;
        }

        // Look up user's role in TenantUsers table
        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id && tu.UserId == userId);

        if (tenantUser == null)
        {
            _logger.LogWarning("User {UserId} not found in TenantUsers for tenant {TenantId}", 
                userId, tenant.Id);
            return;
        }

        // Check if user's role matches any of the allowed roles
        var userRoleName = tenantUser.Role.ToString();
        if (requirement.AllowedRoles.Contains(userRoleName, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogInformation("User {UserId} has role {Role} which is allowed", 
                userId, userRoleName);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning("User {UserId} has role {Role} but requires one of: {AllowedRoles}", 
                userId, userRoleName, string.Join(", ", requirement.AllowedRoles));
        }
    }
}
