using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Attributes;

/// <summary>
/// Attribute to enforce tenant role-based access control
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequiresTenantRoleAttribute : Attribute, IAsyncActionFilter
{
    private readonly TenantRole[] _allowedRoles;
    private readonly string? _operationName;

    /// <summary>
    /// Initializes a new instance of the RequiresTenantRoleAttribute
    /// </summary>
    /// <param name="allowedRoles">Array of tenant roles allowed to access this endpoint</param>
    public RequiresTenantRoleAttribute(params TenantRole[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    /// <summary>
    /// Initializes a new instance with operation name for better error messages
    /// </summary>
    public RequiresTenantRoleAttribute(string operationName, params TenantRole[] allowedRoles)
    {
        _operationName = operationName;
        _allowedRoles = allowedRoles;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get services from DI
        var dbContext = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RequiresTenantRoleAttribute>>();

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

        // Get user and tenant claims
        var tenantIdClaim = context.HttpContext.User.FindFirst("tid")?.Value;
        var userIdClaim = context.HttpContext.User.FindFirst("oid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userIdClaim))
        {
            context.Result = new UnauthorizedObjectResult(ApiResponse<object>.ErrorResponse(
                "AUTH_ERROR",
                "Missing tenant or user claim"));
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

        // Get user's role for this tenant
        var tenantUser = await dbContext.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id 
                                    && tu.EntraIdUserId == userIdClaim 
                                    && tu.IsActive);

        // If no explicit role assignment, check if this is the primary admin (auto-Owner)
        TenantRole userRole;
        if (tenantUser == null)
        {
            // Check if user is the primary admin email
            var userEmail = context.HttpContext.User.FindFirst("upn")?.Value 
                         ?? context.HttpContext.User.FindFirst("email")?.Value;

            if (!string.IsNullOrEmpty(userEmail) && 
                userEmail.Equals(tenant.PrimaryAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                userRole = TenantRole.Owner; // Primary admin automatically gets Owner role
                logger?.LogInformation(
                    "User {UserId} ({Email}) granted Owner role as primary admin for tenant {TenantId}",
                    userIdClaim, userEmail, tenant.Id);
            }
            else
            {
                // Default to Viewer for authenticated users without explicit role
                userRole = TenantRole.Viewer;
                logger?.LogInformation(
                    "User {UserId} has no explicit role for tenant {TenantId}, defaulting to Viewer",
                    userIdClaim, tenant.Id);
            }
        }
        else
        {
            userRole = tenantUser.Role;
        }

        // Check if user's role is in the allowed roles
        if (!_allowedRoles.Contains(userRole))
        {
            var roleNames = string.Join(", ", _allowedRoles.Select(r => r.ToString()));
            var operationDesc = !string.IsNullOrEmpty(_operationName) 
                ? $"Operation '{_operationName}'"
                : "This operation";

            logger?.LogWarning(
                "Access denied: User {UserId} with role {UserRole} attempted to access endpoint requiring {RequiredRoles}",
                userIdClaim, userRole, roleNames);

            context.Result = new ObjectResult(ApiResponse<object>.ErrorResponse(
                "FORBIDDEN",
                $"{operationDesc} requires one of the following roles: {roleNames}. Your role: {userRole}"))
            {
                StatusCode = 403
            };
            return;
        }

        // Store the user role in HttpContext for use in controllers
        context.HttpContext.Items["TenantUserId"] = tenantUser?.Id;
        context.HttpContext.Items["TenantUserRole"] = userRole;
        context.HttpContext.Items["TenantId"] = tenant.Id;

        logger?.LogDebug(
            "User {UserId} with role {UserRole} granted access to endpoint",
            userIdClaim, userRole);

        // All checks passed, continue with the action
        await next();
    }
}
