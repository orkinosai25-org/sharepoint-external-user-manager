using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Clients;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using SharePointExternalUserManager.Functions.Models.Libraries;
using SharePointExternalUserManager.Functions.Models.Lists;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for managing client spaces
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISharePointService _sharePointService;
    private readonly IAuditLogService _auditLogService;
    private readonly IPlanEnforcementService _planEnforcementService;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        IAuditLogService auditLogService,
        IPlanEnforcementService planEnforcementService,
        ILogger<ClientsController> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _auditLogService = auditLogService;
        _planEnforcementService = planEnforcementService;
        _logger = logger;
    }

    /// <summary>
    /// Get all clients for the authenticated tenant
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireViewer")]
    public async Task<IActionResult> GetClients()
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        // Get the internal tenant ID from the database
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found in database"));

        var clients = await _context.Clients
            .Where(c => c.TenantId == tenant.Id && c.IsActive)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new ClientResponse
            {
                Id = c.Id,
                ClientReference = c.ClientReference,
                ClientName = c.ClientName,
                Description = c.Description,
                SharePointSiteId = c.SharePointSiteId,
                SharePointSiteUrl = c.SharePointSiteUrl,
                ProvisioningStatus = c.ProvisioningStatus,
                ProvisionedDate = c.ProvisionedDate,
                ProvisioningError = c.ProvisioningError,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ClientResponse>>.SuccessResponse(clients));
    }

    /// <summary>
    /// Get a specific client by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireViewer")]
    public async Task<IActionResult> GetClient(int id)
    {
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        var client = await _context.Clients
            .Where(c => c.Id == id && c.TenantId == tenant.Id)
            .Select(c => new ClientResponse
            {
                Id = c.Id,
                ClientReference = c.ClientReference,
                ClientName = c.ClientName,
                Description = c.Description,
                SharePointSiteId = c.SharePointSiteId,
                SharePointSiteUrl = c.SharePointSiteUrl,
                ProvisioningStatus = c.ProvisioningStatus,
                ProvisionedDate = c.ProvisionedDate,
                ProvisioningError = c.ProvisioningError,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy
            })
            .FirstOrDefaultAsync();

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        return Ok(ApiResponse<ClientResponse>.SuccessResponse(client));
    }

    /// <summary>
    /// Create a new client space with SharePoint site provisioning
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        // Get the internal tenant from database
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "TENANT_NOT_FOUND",
                "Tenant not found. Please complete onboarding first."));
        }

        // Enforce plan-based client space limits
        try
        {
            await _planEnforcementService.EnforceClientSpaceLimitAsync(tenant.Id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                ex,
                "Plan limit exceeded for tenant {TenantId}. CorrelationId: {CorrelationId}",
                tenant.Id,
                correlationId);

            return StatusCode(403, ApiResponse<object>.ErrorResponse(
                "PLAN_LIMIT_EXCEEDED",
                ex.Message,
                correlationId));
        }

        // Check if client reference already exists for this tenant
        var clientExists = await _context.Clients
            .AnyAsync(c => c.TenantId == tenant.Id && c.ClientReference == request.ClientReference);

        if (clientExists)
        {
            return Conflict(ApiResponse<object>.ErrorResponse(
                "CLIENT_EXISTS",
                $"A client with reference '{request.ClientReference}' already exists"));
        }

        // Create the client entity
        var client = new ClientEntity
        {
            TenantId = tenant.Id,
            ClientReference = request.ClientReference,
            ClientName = request.ClientName,
            Description = request.Description,
            ProvisioningStatus = "Provisioning",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = userEmail ?? userId,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Client {ClientReference} created with ID {ClientId} for tenant {TenantId}",
            client.ClientReference,
            client.Id,
            tenant.Id);

        // Log the client creation
        await _auditLogService.LogActionAsync(
            tenant.Id,
            userId,
            userEmail,
            "CLIENT_CREATED",
            "Client",
            client.Id.ToString(),
            $"Client '{client.ClientName}' ({client.ClientReference}) created",
            ipAddress,
            correlationId,
            "Success");

        // Provision SharePoint site
        try
        {
            var (success, siteId, siteUrl, errorMessage) = await _sharePointService.CreateClientSiteAsync(
                client,
                userEmail ?? "system@unknown.com");

            if (success)
            {
                client.SharePointSiteId = siteId;
                client.SharePointSiteUrl = siteUrl;
                client.ProvisioningStatus = "Provisioned";
                client.ProvisionedDate = DateTime.UtcNow;
                client.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "SharePoint site provisioned for client {ClientReference}: {SiteUrl}",
                    client.ClientReference,
                    siteUrl);

                // Log successful provisioning
                await _auditLogService.LogActionAsync(
                    tenant.Id,
                    userId,
                    userEmail,
                    "SITE_PROVISIONED",
                    "Client",
                    client.Id.ToString(),
                    $"SharePoint site provisioned: {siteUrl}",
                    ipAddress,
                    correlationId,
                    "Success");
            }
            else
            {
                client.ProvisioningStatus = "Failed";
                client.ProvisioningError = errorMessage;
                client.ModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError(
                    "SharePoint site provisioning failed for client {ClientReference}: {ErrorMessage}",
                    client.ClientReference,
                    errorMessage);

                // Log failed provisioning
                await _auditLogService.LogActionAsync(
                    tenant.Id,
                    userId,
                    userEmail,
                    "SITE_PROVISIONING_FAILED",
                    "Client",
                    client.Id.ToString(),
                    $"Site provisioning failed: {errorMessage}",
                    ipAddress,
                    correlationId,
                    "Failed");
            }
        }
        catch (Exception ex)
        {
            client.ProvisioningStatus = "Failed";
            client.ProvisioningError = ex.Message;
            client.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogError(
                ex,
                "Exception during SharePoint site provisioning for client {ClientReference}",
                client.ClientReference);

            // Log exception
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "SITE_PROVISIONING_ERROR",
                "Client",
                client.Id.ToString(),
                $"Provisioning exception: {ex.Message}",
                ipAddress,
                correlationId,
                "Error");
        }

        // Return the created client
        var clientResponse = new ClientResponse
        {
            Id = client.Id,
            ClientReference = client.ClientReference,
            ClientName = client.ClientName,
            Description = client.Description,
            SharePointSiteId = client.SharePointSiteId,
            SharePointSiteUrl = client.SharePointSiteUrl,
            ProvisioningStatus = client.ProvisioningStatus,
            ProvisionedDate = client.ProvisionedDate,
            ProvisioningError = client.ProvisioningError,
            IsActive = client.IsActive,
            CreatedDate = client.CreatedDate,
            CreatedBy = client.CreatedBy
        };

        return CreatedAtAction(
            nameof(GetClient),
            new { id = client.Id },
            ApiResponse<ClientResponse>.SuccessResponse(clientResponse));
    }

    /// <summary>
    /// Get all external users for a client site
    /// </summary>
    [HttpGet("{id}/external-users")]
    [Authorize(Policy = "RequireViewer")]
    public async Task<IActionResult> GetExternalUsers(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        // Get external users from SharePoint
        var externalUsers = await _sharePointService.GetExternalUsersAsync(client.SharePointSiteId);

        _logger.LogInformation(
            "Retrieved {Count} external users for client {ClientId}",
            externalUsers.Count,
            client.Id);

        return Ok(ApiResponse<List<ExternalUserDto>>.SuccessResponse(externalUsers));
    }

    /// <summary>
    /// Invite an external user to a client site
    /// </summary>
    [HttpPost("{id}/external-users")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> InviteExternalUser(int id, [FromBody] InviteExternalUserRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        // Validate permission level
        var validPermissions = new[] { "Read", "Edit", "Write", "Contribute" };
        if (!validPermissions.Contains(request.PermissionLevel, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "INVALID_PERMISSION",
                $"Permission level must be one of: {string.Join(", ", validPermissions)}"));
        }

        // Invite the external user
        var (success, user, errorMessage) = await _sharePointService.InviteExternalUserAsync(
            client.SharePointSiteId,
            request.Email,
            request.DisplayName,
            request.PermissionLevel,
            request.Message,
            userEmail ?? userId);

        if (!success)
        {
            _logger.LogError(
                "Failed to invite external user {Email} to client {ClientId}: {Error}",
                request.Email,
                client.Id,
                errorMessage);

            // Log failure
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "EXTERNAL_USER_INVITE_FAILED",
                "Client",
                client.Id.ToString(),
                $"Failed to invite {request.Email}: {errorMessage}",
                ipAddress,
                correlationId,
                "Failed");

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "INVITE_FAILED",
                errorMessage ?? "Failed to invite external user"));
        }

        _logger.LogInformation(
            "Successfully invited external user {Email} to client {ClientId} with {PermissionLevel} permissions",
            request.Email,
            client.Id,
            request.PermissionLevel);

        // Log successful invitation
        await _auditLogService.LogActionAsync(
            tenant.Id,
            userId,
            userEmail,
            "EXTERNAL_USER_INVITED",
            "Client",
            client.Id.ToString(),
            $"Invited external user {request.Email} with {request.PermissionLevel} permissions",
            ipAddress,
            correlationId,
            "Success");

        return Ok(ApiResponse<ExternalUserDto>.SuccessResponse(user!));
    }

    /// <summary>
    /// Remove an external user from a client site
    /// </summary>
    [HttpDelete("{id}/external-users/{email}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> RemoveExternalUser(int id, string email)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        // Remove the external user
        var (success, errorMessage) = await _sharePointService.RemoveExternalUserAsync(
            client.SharePointSiteId,
            email);

        if (!success)
        {
            _logger.LogError(
                "Failed to remove external user {Email} from client {ClientId}: {Error}",
                email,
                client.Id,
                errorMessage);

            // Log failure
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "EXTERNAL_USER_REMOVE_FAILED",
                "Client",
                client.Id.ToString(),
                $"Failed to remove {email}: {errorMessage}",
                ipAddress,
                correlationId,
                "Failed");

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "REMOVE_FAILED",
                errorMessage ?? "Failed to remove external user"));
        }

        _logger.LogInformation(
            "Successfully removed external user {Email} from client {ClientId}",
            email,
            client.Id);

        // Log successful removal
        await _auditLogService.LogActionAsync(
            tenant.Id,
            userId,
            userEmail,
            "EXTERNAL_USER_REMOVED",
            "Client",
            client.Id.ToString(),
            $"Removed external user {email}",
            ipAddress,
            correlationId,
            "Success");

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            message = $"External user {email} removed successfully"
        }));
    }

    /// <summary>
    /// Get all document libraries for a client site
    /// </summary>
    [HttpGet("{id}/libraries")]
    [Authorize(Policy = "RequireViewer")]
    public async Task<IActionResult> GetLibraries(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        // Get libraries from SharePoint
        var libraries = await _sharePointService.GetLibrariesAsync(client.SharePointSiteId);

        _logger.LogInformation(
            "Retrieved {Count} libraries for client {ClientId}",
            libraries.Count,
            client.Id);

        return Ok(ApiResponse<List<LibraryResponse>>.SuccessResponse(libraries));
    }

    /// <summary>
    /// Create a new document library in a client site
    /// </summary>
    [HttpPost("{id}/libraries")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateLibrary(int id, [FromBody] CreateLibraryRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        if (client.ProvisioningStatus != "Provisioned")
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_ACTIVE",
                "Client site is not in an active state"));
        }

        // Create the library
        try
        {
            var library = await _sharePointService.CreateLibraryAsync(
                client.SharePointSiteId,
                request.Name,
                request.Description);

            _logger.LogInformation(
                "Successfully created library '{LibraryName}' for client {ClientId}",
                request.Name,
                client.Id);

            // Log the library creation
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "LIBRARY_CREATED",
                "Client",
                client.Id.ToString(),
                $"Created library '{request.Name}'",
                ipAddress,
                correlationId,
                "Success");

            return Ok(ApiResponse<LibraryResponse>.SuccessResponse(library));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create library '{LibraryName}' for client {ClientId}",
                request.Name,
                client.Id);

            // Log failure
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "LIBRARY_CREATE_FAILED",
                "Client",
                client.Id.ToString(),
                $"Failed to create library '{request.Name}': {ex.Message}",
                ipAddress,
                correlationId,
                "Failed");

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "CREATE_FAILED",
                ex.Message));
        }
    }

    /// <summary>
    /// Get all lists for a client site
    /// </summary>
    [HttpGet("{id}/lists")]
    [Authorize(Policy = "RequireViewer")]
    public async Task<IActionResult> GetLists(int id)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;

        if (string.IsNullOrEmpty(tenantIdClaim))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing tenant claim"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        // Get lists from SharePoint
        var lists = await _sharePointService.GetListsAsync(client.SharePointSiteId);

        _logger.LogInformation(
            "Retrieved {Count} lists for client {ClientId}",
            lists.Count,
            client.Id);

        return Ok(ApiResponse<List<ListResponse>>.SuccessResponse(lists));
    }

    /// <summary>
    /// Create a new list in a client site
    /// </summary>
    [HttpPost("{id}/lists")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateList(int id, [FromBody] CreateListRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantIdClaim = User.FindFirst("tid")?.Value;
        var userId = User.FindFirst("oid")?.Value;
        var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrEmpty(tenantIdClaim) || string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("AUTH_ERROR", "Missing authentication claims"));

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantIdClaim);

        if (tenant == null)
            return NotFound(ApiResponse<object>.ErrorResponse("TENANT_NOT_FOUND", "Tenant not found"));

        // Get the client and verify tenant ownership
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

        if (client == null)
            return NotFound(ApiResponse<object>.ErrorResponse("CLIENT_NOT_FOUND", "Client not found"));

        if (string.IsNullOrEmpty(client.SharePointSiteId))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_PROVISIONED",
                "Client site has not been provisioned yet"));
        }

        if (client.ProvisioningStatus != "Provisioned")
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "SITE_NOT_ACTIVE",
                "Client site is not in an active state"));
        }

        // Create the list
        try
        {
            var list = await _sharePointService.CreateListAsync(
                client.SharePointSiteId,
                request.Name,
                request.Description,
                request.Template);

            _logger.LogInformation(
                "Successfully created list '{ListName}' for client {ClientId}",
                request.Name,
                client.Id);

            // Log the list creation
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "LIST_CREATED",
                "Client",
                client.Id.ToString(),
                $"Created list '{request.Name}' with template '{request.Template ?? "genericList"}'",
                ipAddress,
                correlationId,
                "Success");

            return Ok(ApiResponse<ListResponse>.SuccessResponse(list));
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create list '{ListName}' for client {ClientId}",
                request.Name,
                client.Id);

            // Log failure
            await _auditLogService.LogActionAsync(
                tenant.Id,
                userId,
                userEmail,
                "LIST_CREATE_FAILED",
                "Client",
                client.Id.ToString(),
                $"Failed to create list '{request.Name}': {ex.Message}",
                ipAddress,
                correlationId,
                "Failed");

            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "CREATE_FAILED",
                ex.Message));
        }
    }
}
