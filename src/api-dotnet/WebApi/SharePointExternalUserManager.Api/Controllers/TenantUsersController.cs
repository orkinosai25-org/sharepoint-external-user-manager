using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for managing tenant user roles (RBAC)
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class TenantUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenantUsersController> _logger;

    public TenantUsersController(
        ApplicationDbContext context,
        ILogger<TenantUsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all users in the tenant with their roles
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetTenantUsers()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        var tenantUsers = await _context.TenantUsers
            .Where(tu => tu.TenantId == tenant.Id)
            .OrderBy(tu => tu.Email)
            .Select(tu => new TenantUserResponse
            {
                Id = tu.Id,
                UserId = tu.UserId,
                Email = tu.Email,
                DisplayName = tu.DisplayName,
                Role = tu.Role.ToString(),
                CreatedDate = tu.CreatedDate,
                CreatedBy = tu.CreatedBy
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TenantUserResponse>>.SuccessResponse(tenantUsers));
    }

    /// <summary>
    /// Get current user's role in the tenant
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRole()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var email = User.FindFirst("email")?.Value ?? User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Check if user is primary admin (automatic TenantOwner)
        if (tenant.PrimaryAdminEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(ApiResponse<TenantUserResponse>.SuccessResponse(new TenantUserResponse
            {
                Id = 0,
                UserId = userId,
                Email = email ?? "",
                DisplayName = User.FindFirst("name")?.Value,
                Role = "TenantOwner",
                CreatedDate = tenant.OnboardedDate,
                CreatedBy = "System"
            }));
        }

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id && tu.UserId == userId);

        if (tenantUser == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "USER_NOT_FOUND",
                "You are not assigned to this tenant. Please contact your tenant administrator."));
        }

        return Ok(ApiResponse<TenantUserResponse>.SuccessResponse(new TenantUserResponse
        {
            Id = tenantUser.Id,
            UserId = tenantUser.UserId,
            Email = tenantUser.Email,
            DisplayName = tenantUser.DisplayName,
            Role = tenantUser.Role.ToString(),
            CreatedDate = tenantUser.CreatedDate,
            CreatedBy = tenantUser.CreatedBy
        }));
    }

    /// <summary>
    /// Add a user to the tenant with a specific role
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireOwner")]
    public async Task<IActionResult> AddTenantUser([FromBody] AddTenantUserRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var currentUserId = User.FindFirst("oid")?.Value;
        var currentUserEmail = User.FindFirst("email")?.Value ?? User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(currentUserId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Validate role
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "INVALID_ROLE",
                $"Invalid role: {request.Role}. Valid roles are: TenantOwner, TenantAdmin, Viewer"));
        }

        // Check if user already exists in tenant
        var existingUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == tenant.Id && tu.UserId == request.UserId);

        if (existingUser != null)
        {
            return Conflict(ApiResponse<object>.ErrorResponse(
                "USER_EXISTS",
                $"User {request.Email} is already a member of this tenant with role {existingUser.Role}"));
        }

        var tenantUser = new TenantUserEntity
        {
            TenantId = tenant.Id,
            UserId = request.UserId,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = role,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserEmail ?? currentUserId,
            ModifiedDate = DateTime.UtcNow
        };

        _context.TenantUsers.Add(tenantUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Email} added to tenant {TenantId} with role {Role} by {CurrentUser}. CorrelationId: {CorrelationId}",
            request.Email,
            tenant.Id,
            role,
            currentUserEmail,
            correlationId);

        return Ok(ApiResponse<TenantUserResponse>.SuccessResponse(new TenantUserResponse
        {
            Id = tenantUser.Id,
            UserId = tenantUser.UserId,
            Email = tenantUser.Email,
            DisplayName = tenantUser.DisplayName,
            Role = tenantUser.Role.ToString(),
            CreatedDate = tenantUser.CreatedDate,
            CreatedBy = tenantUser.CreatedBy
        }));
    }

    /// <summary>
    /// Update a user's role in the tenant
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireOwner")]
    public async Task<IActionResult> UpdateTenantUser(int id, [FromBody] UpdateTenantUserRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var currentUserId = User.FindFirst("oid")?.Value;
        var currentUserEmail = User.FindFirst("email")?.Value ?? User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(currentUserId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.Id == id && tu.TenantId == tenant.Id);

        if (tenantUser == null)
            return NotFound(ApiResponse<object>.ErrorResponse("USER_NOT_FOUND", "Tenant user not found"));

        // Validate role
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "INVALID_ROLE",
                $"Invalid role: {request.Role}. Valid roles are: TenantOwner, TenantAdmin, Viewer"));
        }

        tenantUser.Role = role;
        tenantUser.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Email} role updated to {Role} in tenant {TenantId} by {CurrentUser}. CorrelationId: {CorrelationId}",
            tenantUser.Email,
            role,
            tenant.Id,
            currentUserEmail,
            correlationId);

        return Ok(ApiResponse<TenantUserResponse>.SuccessResponse(new TenantUserResponse
        {
            Id = tenantUser.Id,
            UserId = tenantUser.UserId,
            Email = tenantUser.Email,
            DisplayName = tenantUser.DisplayName,
            Role = tenantUser.Role.ToString(),
            CreatedDate = tenantUser.CreatedDate,
            CreatedBy = tenantUser.CreatedBy
        }));
    }

    /// <summary>
    /// Remove a user from the tenant
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireOwner")]
    public async Task<IActionResult> RemoveTenantUser(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var currentUserId = User.FindFirst("oid")?.Value;
        var currentUserEmail = User.FindFirst("email")?.Value ?? User.FindFirst("upn")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(currentUserId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        var tenantUser = await _context.TenantUsers
            .FirstOrDefaultAsync(tu => tu.Id == id && tu.TenantId == tenant.Id);

        if (tenantUser == null)
            return NotFound(ApiResponse<object>.ErrorResponse("USER_NOT_FOUND", "Tenant user not found"));

        // Prevent user from removing themselves
        if (tenantUser.UserId == currentUserId)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "CANNOT_REMOVE_SELF",
                "You cannot remove yourself from the tenant"));
        }

        _context.TenantUsers.Remove(tenantUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {Email} removed from tenant {TenantId} by {CurrentUser}. CorrelationId: {CorrelationId}",
            tenantUser.Email,
            tenant.Id,
            currentUserEmail,
            correlationId);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = $"User {tenantUser.Email} removed from tenant"
        }));
    }
}

/// <summary>
/// Response model for tenant user information
/// </summary>
public class TenantUserResponse
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Request model for adding a user to tenant
/// </summary>
public class AddTenantUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating a user's role
/// </summary>
public class UpdateTenantUserRequest
{
    public string Role { get; set; } = string.Empty;
}
