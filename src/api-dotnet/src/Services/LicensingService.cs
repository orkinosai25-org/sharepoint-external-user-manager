using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Middleware;

namespace SharePointExternalUserManager.Functions.Services;

public interface ILicensingService
{
    Task<SubscriptionInfo?> GetSubscriptionStatusAsync(Guid tenantId);
    Task<bool> IsFeatureAvailableAsync(Guid tenantId, string featureName);
    Task<bool> CheckResourceLimitAsync(Guid tenantId, string resourceType, int requestedCount);
}

public class LicensingService : ILicensingService
{
    // Feature gates by tier
    private readonly Dictionary<SubscriptionTier, HashSet<string>> _tierFeatures = new()
    {
        {
            SubscriptionTier.Free,
            new HashSet<string>
            {
                "GetLibraries",
                "GetLibraryUsers",
                "InviteUser",
                "ClientSpaceSearch"
            }
        },
        {
            SubscriptionTier.Pro,
            new HashSet<string>
            {
                "GetLibraries",
                "CreateLibrary",
                "DeleteLibrary",
                "GetLibraryUsers",
                "InviteUser",
                "RevokeUserAccess",
                "UpdateUserPermissions",
                "GetPolicies",
                "UpdatePolicies",
                "QueryAuditLogs",
                "ClientSpaceSearch",
                "GlobalSearch"
            }
        },
        {
            SubscriptionTier.Enterprise,
            new HashSet<string>
            {
                // All features available
                "GetLibraries",
                "CreateLibrary",
                "DeleteLibrary",
                "GetLibraryUsers",
                "InviteUser",
                "RevokeUserAccess",
                "UpdateUserPermissions",
                "GetPolicies",
                "CreatePolicy",
                "UpdatePolicies",
                "DeletePolicy",
                "QueryAuditLogs",
                "ExportAuditLogs",
                "BulkUserOperations",
                "CustomIntegrations",
                "ClientSpaceSearch",
                "GlobalSearch"
            }
        }
    };

    // Resource limits by tier
    private readonly Dictionary<SubscriptionTier, Dictionary<string, int>> _tierLimits = new()
    {
        {
            SubscriptionTier.Free,
            new Dictionary<string, int>
            {
                { "MaxUsers", 5 },
                { "MaxLibraries", 3 },
                { "APICallsPerMinute", 60 }
            }
        },
        {
            SubscriptionTier.Pro,
            new Dictionary<string, int>
            {
                { "MaxUsers", 50 },
                { "MaxLibraries", 25 },
                { "APICallsPerMinute", 300 }
            }
        },
        {
            SubscriptionTier.Enterprise,
            new Dictionary<string, int>
            {
                { "MaxUsers", int.MaxValue },
                { "MaxLibraries", int.MaxValue },
                { "APICallsPerMinute", 1000 }
            }
        }
    };

    public async Task<SubscriptionInfo?> GetSubscriptionStatusAsync(Guid tenantId)
    {
        // TODO: Implement database lookup
        // For MVP, return mock data
        await Task.CompletedTask;
        
        return new SubscriptionInfo
        {
            TenantId = tenantId,
            Tier = SubscriptionTier.Pro,
            Status = SubscriptionStatus.Active,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };
    }

    public async Task<bool> IsFeatureAvailableAsync(Guid tenantId, string featureName)
    {
        var subscription = await GetSubscriptionStatusAsync(tenantId);
        
        if (subscription == null)
            return false;

        if (!_tierFeatures.ContainsKey(subscription.Tier))
            return false;

        return _tierFeatures[subscription.Tier].Contains(featureName);
    }

    public async Task<bool> CheckResourceLimitAsync(Guid tenantId, string resourceType, int requestedCount)
    {
        var subscription = await GetSubscriptionStatusAsync(tenantId);
        
        if (subscription == null)
            return false;

        if (!_tierLimits.ContainsKey(subscription.Tier))
            return false;

        var limits = _tierLimits[subscription.Tier];
        
        if (!limits.ContainsKey(resourceType))
            return true; // No limit defined

        var maxAllowed = limits[resourceType];
        
        // TODO: Get current usage from database
        var currentUsage = 0;
        
        return (currentUsage + requestedCount) <= maxAllowed;
    }
}
