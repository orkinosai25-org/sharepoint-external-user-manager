using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Services;

namespace SharePointExternalUserManager.Functions.Middleware;

public class LicenseEnforcementMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<LicenseEnforcementMiddleware> _logger;
    private readonly ILicensingService _licensingService;

    public LicenseEnforcementMiddleware(
        ILogger<LicenseEnforcementMiddleware> logger,
        ILicensingService licensingService)
    {
        _logger = logger;
        _licensingService = licensingService;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        
        if (requestData != null)
        {
            try
            {
                // Get tenant ID from context (set by AuthenticationMiddleware)
                var tenantIdString = context.Items["TenantId"] as string;
                
                if (string.IsNullOrEmpty(tenantIdString))
                {
                    _logger.LogWarning("Tenant ID not found in context");
                    await next(context);
                    return;
                }

                var tenantId = Guid.Parse(tenantIdString);

                // Check subscription status
                var subscription = await _licensingService.GetSubscriptionStatusAsync(tenantId);

                if (subscription == null)
                {
                    _logger.LogWarning("Tenant subscription not found: {TenantId}", tenantId);
                    await WritePaymentRequiredResponse(requestData, "Tenant subscription not found");
                    return;
                }

                // Check if subscription is active
                if (subscription.Status == SubscriptionStatus.Expired)
                {
                    // Check if within grace period
                    if (subscription.EndDate.HasValue && 
                        DateTime.UtcNow > subscription.EndDate.Value.AddDays(7))
                    {
                        _logger.LogWarning("Subscription expired for tenant: {TenantId}", tenantId);
                        await WritePaymentRequiredResponse(requestData, "Subscription has expired");
                        return;
                    }
                }

                if (subscription.Status == SubscriptionStatus.Suspended || 
                    subscription.Status == SubscriptionStatus.Cancelled)
                {
                    _logger.LogWarning("Subscription {Status} for tenant: {TenantId}", subscription.Status, tenantId);
                    await WritePaymentRequiredResponse(requestData, $"Subscription is {subscription.Status.ToString().ToLower()}");
                    return;
                }

                // Check feature access based on tier
                var functionName = context.FunctionDefinition.Name;
                if (!await _licensingService.IsFeatureAvailableAsync(tenantId, functionName))
                {
                    _logger.LogWarning("Feature {FunctionName} not available for tenant {TenantId} tier {Tier}", 
                        functionName, tenantId, subscription.Tier);
                    await WriteForbiddenResponse(requestData, "Feature not available in current subscription tier");
                    return;
                }

                // Store subscription info in context
                context.Items["SubscriptionTier"] = subscription.Tier;
                context.Items["SubscriptionStatus"] = subscription.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "License enforcement middleware error");
            }
        }

        await next(context);
    }

    private async Task WritePaymentRequiredResponse(HttpRequestData request, string message)
    {
        var response = request.CreateResponse((System.Net.HttpStatusCode)402); // Payment Required
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(
            "PAYMENT_REQUIRED",
            message,
            "Please update your subscription to continue using this service"
        ));
        
        var context = request.FunctionContext;
        context.GetInvocationResult().Value = response;
    }

    private async Task WriteForbiddenResponse(HttpRequestData request, string message)
    {
        var response = request.CreateResponse(System.Net.HttpStatusCode.Forbidden);
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(
            "FORBIDDEN",
            message,
            "Upgrade your subscription to access this feature"
        ));
        
        var context = request.FunctionContext;
        context.GetInvocationResult().Value = response;
    }
}

public class SubscriptionInfo
{
    public Guid TenantId { get; set; }
    public SubscriptionTier Tier { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime? EndDate { get; set; }
}
