using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Implements just-in-time tenant provisioning.
/// The first time an authenticated user reaches the API their Entra ID tenant is
/// registered automatically so that dashboard, clients, and other endpoints return
/// meaningful data instead of TENANT_NOT_FOUND 404 responses.
/// </summary>
public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantUserService _tenantUserService;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        ApplicationDbContext context,
        ITenantUserService tenantUserService,
        ILogger<TenantProvisioningService> logger)
    {
        _context = context;
        _tenantUserService = tenantUserService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TenantEntity> GetOrCreateTenantAsync(
        string entraIdTenantId,
        string? userId,
        string? userEmail,
        string? userName)
    {
        // Fast path – tenant already exists
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == entraIdTenantId);

        if (tenant != null)
        {
            return tenant;
        }

        _logger.LogInformation(
            "Auto-provisioning tenant for Entra ID {EntraIdTenantId}", entraIdTenantId);

        var orgName = DeriveOrganizationName(userEmail);
        var adminEmail = NormalizeEmail(userEmail);

        tenant = new TenantEntity
        {
            EntraIdTenantId = entraIdTenantId,
            OrganizationName = orgName,
            PrimaryAdminEmail = adminEmail,
            Status = "Active",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            OnboardedDate = DateTime.UtcNow,
            Settings = System.Text.Json.JsonSerializer.Serialize(new TenantSettings())
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Create a default Free subscription with a 30-day trial so new tenants can
        // immediately explore the product without going through the billing flow first.
        var subscription = new SubscriptionEntity
        {
            TenantId = tenant.Id,
            Tier = "Free",
            Status = "Trial",
            StartDate = DateTime.UtcNow,
            EndDate = null,
            TrialExpiry = DateTime.UtcNow.AddDays(30),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Assign TenantOwner role to the user that triggered auto-provisioning
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                var assignRoleRequest = new AssignRoleRequest
                {
                    AzureAdObjectId = userId,
                    UserPrincipalName = userEmail ?? string.Empty,
                    DisplayName = userName,
                    Role = TenantRole.TenantOwner
                };

                await _tenantUserService.AssignRoleAsync(
                    tenant.Id, assignRoleRequest, userId);

                _logger.LogInformation(
                    "Assigned TenantOwner role to user {UserId} for auto-provisioned tenant {TenantId}",
                    userId, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to assign TenantOwner role during auto-provisioning for tenant {TenantId}",
                    tenant.Id);
                // Non-fatal: the tenant and subscription were created successfully.
            }
        }

        // Reload so callers receive the full entity with navigation properties populated
        tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
            .FirstAsync(t => t.Id == tenant.Id);

        _logger.LogInformation(
            "Tenant {TenantId} (Entra ID: {EntraIdTenantId}) auto-provisioned successfully",
            tenant.Id, entraIdTenantId);

        return tenant;
    }

    /// <summary>
    /// Derives a human-readable organization name from the user's email address.
    /// For example <c>alice@contoso.com</c> → <c>Contoso</c>.
    /// Falls back to <c>"New Organization"</c> when no useful information is available.
    /// </summary>
    private static string DeriveOrganizationName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "New Organization";
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return "New Organization";
        }

        var domain = email[(atIndex + 1)..];
        var label = domain.Split('.').FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(label))
        {
            return "New Organization";
        }

        // Capitalize the first character; label[1..] is safe because a single-character
        // label produces an empty string rather than an IndexOutOfRangeException.
        return char.ToUpper(label[0]) + (label.Length > 1 ? label[1..].ToLower() : string.Empty);
    }

    /// <summary>
    /// Returns a valid email address for use as <c>PrimaryAdminEmail</c>.
    /// Falls back to a placeholder when the supplied value is null or empty.
    /// </summary>
    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? "admin@unknown.example.com" : email;
}
