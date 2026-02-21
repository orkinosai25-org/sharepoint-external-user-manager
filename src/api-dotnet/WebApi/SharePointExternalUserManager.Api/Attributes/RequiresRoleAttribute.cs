using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Attributes;

/// <summary>
/// Attribute to enforce role-based access control at tenant level
/// Validates that the authenticated user has the required role within the tenant
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresRoleAttribute : Attribute, IAsyncActionFilter
{
    private readonly TenantRole _minimumRole;

    /// <summary>
    /// Initializes a new instance of the RequiresRoleAttribute
    /// </summary>
    /// <param name="minimumRole">Minimum role required to access this resource</param>
    public RequiresRoleAttribute(TenantRole minimumRole)
    {
        _minimumRole = minimumRole;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get services from DI
        var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RequiresRoleAttribute>>();

        if (dbContext == null)
        {
            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "INTERNAL_ERROR",
                "Database context not available"))
            {
                StatusCode = 500
            };
            return;
        }

        // Get tenant ID and user ID from claims
        var tenantIdClaim = context.HttpContext.User.FindFirst("tid")?.Value;
        var userObjectId = context.HttpContext.User.FindFirst("oid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant claim"));
            return;
        }

        if (string.IsNullOrEmpty(userObjectId))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing user claim"));
            return;
        }

        // Get tenant from database
        var tenant = await dbContext.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
        {
            context.Result = new NotFoundObjectResult(ApiResponse<object>.ErrorResponse(
                "TENANT_NOT_FOUND",
                "Tenant not found"));
            return;
        }

        // Get user's role in this tenant
        var tenantUser = await dbContext.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id && 
                                      tu.AzureAdObjectId == userObjectId &&
                                      tu.IsActive);

        if (tenantUser == null)
        {
            logger?.LogWarning(
                "User {UserId} attempted to access tenant {TenantId} without assigned role",
                userObjectId, tenantIdClaim);

            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "ACCESS_DENIED",
                "You do not have access to this tenant. Please contact your tenant administrator."))
            {
                StatusCode = 403
            };
            return;
        }

        // Check if user's role meets minimum requirement
        if (tenantUser.Role < _minimumRole)
        {
            logger?.LogWarning(
                "Role check failed: User {UserId} with role {CurrentRole} attempted to access {RequiredRole} resource in tenant {TenantId}",
                userObjectId, tenantUser.Role, _minimumRole, tenantIdClaim);

            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "INSUFFICIENT_PERMISSIONS",
                $"This operation requires {_minimumRole} role or higher. Your current role: {tenantUser.Role}"))
            {
                StatusCode = 403
            };
            return;
        }

        // All checks passed, continue with the action
        logger?.LogDebug(
            "Role check passed: User {UserId} with role {Role} accessing tenant {TenantId}",
            userObjectId, tenantUser.Role, tenantIdClaim);

        await next();
    }
}
