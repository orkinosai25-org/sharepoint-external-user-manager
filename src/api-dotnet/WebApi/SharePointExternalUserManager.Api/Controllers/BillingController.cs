using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;
using Stripe;
using System.Security.Claims;

namespace SharePointExternalUserManager.Api.Controllers;

/// <summary>
/// Controller for billing and subscription management
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        ApplicationDbContext context,
        IStripeService stripeService,
        IAuditLogService auditLogService,
        ILogger<BillingController> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get list of available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public ActionResult<PlansResponse> GetPlans([FromQuery] bool includeEnterprise = false)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            var plans = PlanConfiguration.GetAvailablePlans(includeEnterprise);
            
            return Ok(new PlansResponse { Plans = plans });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve plans. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to retrieve plans", correlationId });
        }
    }

    /// <summary>
    /// Create a Stripe checkout session for a plan
    /// </summary>
    [HttpPost("checkout-session")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Get tenant ID from claims
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? User.FindFirst("tid")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Tenant ID not found in token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(new { error = "Tenant ID not found in token", correlationId });
            }

            // Enterprise plans require custom sales process
            if (request.PlanTier == SubscriptionTier.Enterprise)
            {
                return BadRequest(new
                {
                    error = "Enterprise plans require custom pricing. Please contact sales.",
                    correlationId
                });
            }

            // Validate URLs
            if (string.IsNullOrEmpty(request.SuccessUrl) || string.IsNullOrEmpty(request.CancelUrl))
            {
                return BadRequest(new { error = "Success and cancel URLs are required", correlationId });
            }

            // Create checkout session
            var session = await _stripeService.CreateCheckoutSessionAsync(
                tenantId,
                request.PlanTier,
                request.IsAnnual,
                request.SuccessUrl,
                request.CancelUrl);

            // Get tenant ID for audit log
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
            
            if (tenant != null)
            {
                // Log the action
                await _auditLogService.LogActionAsync(
                    tenant.Id,
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(ClaimTypes.Email)?.Value,
                    "CreateCheckoutSession",
                    "Billing",
                    session.Id,
                    $"Created checkout session for {request.PlanTier} plan",
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    correlationId,
                    "Success");
            }

            return Ok(new CreateCheckoutSessionResponse
            {
                SessionId = session.Id,
                CheckoutUrl = session.Url
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to create checkout session", correlationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkout session. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to create checkout session", correlationId });
        }
    }

    /// <summary>
    /// Get current subscription status for the tenant
    /// </summary>
    [HttpGet("subscription/status")]
    public async Task<ActionResult<SubscriptionStatusResponse>> GetSubscriptionStatus()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Get tenant ID from claims
            var tenantId = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
                ?? User.FindFirst("tid")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                _logger.LogWarning("Tenant ID not found in token. CorrelationId: {CorrelationId}", correlationId);
                return Unauthorized(new { error = "Tenant ID not found in token", correlationId });
            }

            // Get tenant with subscription
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found", correlationId });
            }

            // Get active subscription
            var subscription = tenant.Subscriptions
                .Where(s => s.Status == "Active" || s.Status == "Trial")
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (subscription == null)
            {
                // Return default starter plan
                return Ok(new SubscriptionStatusResponse
                {
                    Tier = "Starter",
                    Status = "None",
                    IsActive = false,
                    Limits = PlanConfiguration.GetPlanDefinition(SubscriptionTier.Starter).Limits,
                    Features = PlanConfiguration.GetPlanDefinition(SubscriptionTier.Starter).Features
                });
            }

            // Get plan definition
            var planDef = PlanConfiguration.GetPlanDefinitionByName(subscription.Tier)
                ?? PlanConfiguration.GetPlanDefinition(SubscriptionTier.Starter);

            return Ok(new SubscriptionStatusResponse
            {
                Tier = subscription.Tier,
                Status = subscription.Status,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                TrialExpiry = subscription.TrialExpiry,
                IsActive = subscription.Status == "Active" || subscription.Status == "Trial",
                StripeSubscriptionId = subscription.StripeSubscriptionId,
                StripeCustomerId = subscription.StripeCustomerId,
                Limits = planDef.Limits,
                Features = planDef.Features
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription status. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to retrieve subscription status", correlationId });
        }
    }

    /// <summary>
    /// Webhook endpoint for Stripe events
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogWarning("Webhook received without signature. CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(new { error = "Missing signature", correlationId });
            }

            // Verify webhook signature
            var stripeEvent = _stripeService.VerifyWebhookSignature(json, stripeSignature);
            if (stripeEvent == null)
            {
                _logger.LogWarning("Webhook signature verification failed. CorrelationId: {CorrelationId}", correlationId);
                return BadRequest(new { error = "Invalid signature", correlationId });
            }

            _logger.LogInformation(
                "Processing webhook event {EventId} type {EventType}. CorrelationId: {CorrelationId}",
                stripeEvent.Id, stripeEvent.Type, correlationId);

            // Handle different event types
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent, correlationId);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent, correlationId);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent, correlationId);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaid(stripeEvent, correlationId);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent, correlationId);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to process webhook", correlationId });
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, string correlationId)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        var tenantId = session.Metadata.GetValueOrDefault("tenant_id");
        var planTierStr = session.Metadata.GetValueOrDefault("plan_tier");

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(planTierStr))
        {
            _logger.LogWarning("Checkout session missing metadata. SessionId: {SessionId}, CorrelationId: {CorrelationId}",
                session.Id, correlationId);
            return;
        }

        if (!Enum.TryParse<SubscriptionTier>(planTierStr, out var planTier))
        {
            _logger.LogWarning("Invalid plan tier in metadata: {PlanTier}. CorrelationId: {CorrelationId}",
                planTierStr, correlationId);
            return;
        }

        // Get or create tenant
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        if (tenant == null)
        {
            _logger.LogWarning("Tenant not found for checkout session. TenantId: {TenantId}, CorrelationId: {CorrelationId}",
                tenantId, correlationId);
            return;
        }

        // Create or update subscription
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.StripeSubscriptionId == session.SubscriptionId);

        if (subscription == null)
        {
            subscription = new Data.Entities.SubscriptionEntity
            {
                TenantId = tenant.Id,
                Tier = planTier.ToString(),
                Status = "Active",
                StartDate = DateTime.UtcNow,
                StripeSubscriptionId = session.SubscriptionId,
                StripeCustomerId = session.CustomerId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Status = "Active";
            subscription.Tier = planTier.ToString();
            subscription.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Log the action
        await _auditLogService.LogActionAsync(
            tenant.Id,
            "system",
            null,
            "SubscriptionActivated",
            "Billing",
            session.SubscriptionId,
            $"Subscription activated for {planTier} plan",
            null,
            correlationId,
            "Success");

        _logger.LogInformation(
            "Activated subscription for tenant {TenantId} plan {PlanTier}. CorrelationId: {CorrelationId}",
            tenantId, planTier, correlationId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent, string correlationId)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        var tenantId = subscription.Metadata.GetValueOrDefault("tenant_id");
        if (string.IsNullOrEmpty(tenantId))
        {
            _logger.LogWarning("Subscription missing tenant_id metadata. SubscriptionId: {SubscriptionId}, CorrelationId: {CorrelationId}",
                subscription.Id, correlationId);
            return;
        }

        // Find subscription record
        var dbSubscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);

        if (dbSubscription == null)
        {
            _logger.LogWarning("Subscription not found in database. SubscriptionId: {SubscriptionId}, CorrelationId: {CorrelationId}",
                subscription.Id, correlationId);
            return;
        }

        // Update status
        dbSubscription.Status = subscription.Status switch
        {
            "active" => "Active",
            "trialing" => "Trial",
            "canceled" => "Cancelled",
            "past_due" => "Suspended",
            _ => dbSubscription.Status
        };

        dbSubscription.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated subscription {SubscriptionId} status to {Status}. CorrelationId: {CorrelationId}",
            subscription.Id, dbSubscription.Status, correlationId);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent, string correlationId)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null) return;

        // Find subscription record
        var dbSubscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id);

        if (dbSubscription == null)
        {
            _logger.LogWarning("Subscription not found in database. SubscriptionId: {SubscriptionId}, CorrelationId: {CorrelationId}",
                subscription.Id, correlationId);
            return;
        }

        // Set to cancelled with grace period
        dbSubscription.Status = "Cancelled";
        dbSubscription.EndDate = DateTime.UtcNow;
        dbSubscription.GracePeriodEnd = DateTime.UtcNow.AddDays(7); // 7-day grace period
        dbSubscription.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log the action
        await _auditLogService.LogActionAsync(
            dbSubscription.Tenant.Id,
            "system",
            null,
            "SubscriptionCancelled",
            "Billing",
            subscription.Id,
            "Subscription cancelled",
            null,
            correlationId,
            "Success");

        _logger.LogInformation(
            "Cancelled subscription {SubscriptionId}. CorrelationId: {CorrelationId}",
            subscription.Id, correlationId);
    }

    private async Task HandleInvoicePaid(Event stripeEvent, string correlationId)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        // Get subscription ID from metadata or expand
        var subscriptionId = invoice.Metadata?.GetValueOrDefault("subscription_id");
        
        if (string.IsNullOrEmpty(subscriptionId))
        {
            // Try to get from the raw JSON data
            var jsonInvoice = stripeEvent.Data.RawObject;
            subscriptionId = jsonInvoice.GetValueOrDefault("subscription") as string;
        }
        
        if (string.IsNullOrEmpty(subscriptionId)) return;

        // Find subscription and update status
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = "Active";
            subscription.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Invoice paid for subscription {SubscriptionId}. CorrelationId: {CorrelationId}",
                subscriptionId, correlationId);
        }
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent, string correlationId)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        // Get subscription ID from metadata or expand
        var subscriptionId = invoice.Metadata?.GetValueOrDefault("subscription_id");
        
        if (string.IsNullOrEmpty(subscriptionId))
        {
            // Try to get from the raw JSON data
            var jsonInvoice = stripeEvent.Data.RawObject;
            subscriptionId = jsonInvoice.GetValueOrDefault("subscription") as string;
        }
        
        if (string.IsNullOrEmpty(subscriptionId)) return;

        // Find subscription and update status
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = "Suspended";
            subscription.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                subscription.Tenant.Id,
                "system",
                null,
                "PaymentFailed",
                "Billing",
                subscriptionId,
                "Payment failed for subscription",
                null,
                correlationId,
                "Failed");

            _logger.LogWarning(
                "Payment failed for subscription {SubscriptionId}. CorrelationId: {CorrelationId}",
                subscriptionId, correlationId);
        }
    }
}
