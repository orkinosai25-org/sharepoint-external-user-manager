using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Services;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Clients;

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
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(
        ApplicationDbContext context,
        ISharePointService sharePointService,
        IAuditLogService auditLogService,
        ILogger<ClientsController> logger)
    {
        _context = context;
        _sharePointService = sharePointService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get all clients for the authenticated tenant
    /// </summary>
    [HttpGet]
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

        // Check if client reference already exists for this tenant
        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.ClientReference == request.ClientReference);

        if (existingClient != null)
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
}
