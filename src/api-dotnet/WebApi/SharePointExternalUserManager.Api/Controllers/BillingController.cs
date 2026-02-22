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
    // Fallback email for auto-created tenants (should rarely be used)
    private const string FallbackTenantEmail = "unknown@example.com";
    
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
            var userEmail = User.FindFirst("upn")?.Value ?? User.FindFirst("email")?.Value;

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

            // Get or create tenant
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found. Please complete onboarding first.", correlationId });
            }

            // Check if tenant has a Stripe customer ID, if not create one
            var activeSubscription = tenant.Subscriptions
                .Where(s => !string.IsNullOrEmpty(s.StripeCustomerId))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            string? customerId = activeSubscription?.StripeCustomerId;

            // If no customer exists, create one
            if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(userEmail))
            {
                try
                {
                    var customer = await _stripeService.CreateCustomerAsync(
                        tenantId,
                        userEmail,
                        tenant.OrganizationName,
                        new Dictionary<string, string>
                        {
                            { "tenant_name", tenant.OrganizationName ?? "" }
                        });

                    customerId = customer.Id;
                    
                    _logger.LogInformation(
                        "Created Stripe customer {CustomerId} for tenant {TenantId}. CorrelationId: {CorrelationId}",
                        customerId, tenantId, correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create Stripe customer. CorrelationId: {CorrelationId}", correlationId);
                    // Continue without customer ID - Stripe will create one during checkout
                }
            }

            // Create checkout session
            var session = await _stripeService.CreateCheckoutSessionAsync(
                tenantId,
                request.PlanTier,
                request.IsAnnual,
                request.SuccessUrl,
                request.CancelUrl);

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
    /// Create a customer portal session for subscription management
    /// </summary>
    [HttpPost("customer-portal")]
    public async Task<ActionResult<CustomerPortalResponse>> CreateCustomerPortal([FromBody] CustomerPortalRequest request)
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

            // Validate return URL
            if (string.IsNullOrEmpty(request.ReturnUrl))
            {
                return BadRequest(new { error = "Return URL is required", correlationId });
            }

            // Get tenant with subscription
            var tenant = await _context.Tenants
                .Include(t => t.Subscriptions)
                .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);

            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found", correlationId });
            }

            // Get active subscription with Stripe customer ID
            var subscription = tenant.Subscriptions
                .Where(s => !string.IsNullOrEmpty(s.StripeCustomerId))
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefault();

            if (subscription == null || string.IsNullOrEmpty(subscription.StripeCustomerId))
            {
                return BadRequest(new 
                { 
                    error = "No Stripe customer found. Please subscribe to a plan first.", 
                    correlationId 
                });
            }

            // Create customer portal session
            var portalSession = await _stripeService.CreateCustomerPortalSessionAsync(
                subscription.StripeCustomerId,
                request.ReturnUrl);

            // Log the action
            await _auditLogService.LogActionAsync(
                tenant.Id,
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                User.FindFirst(ClaimTypes.Email)?.Value,
                "CreateCustomerPortal",
                "Billing",
                portalSession.Id,
                "Created customer portal session",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                correlationId,
                "Success");

            return Ok(new CustomerPortalResponse
            {
                PortalUrl = portalSession.Url
            });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer portal. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to create customer portal session", correlationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create customer portal. CorrelationId: {CorrelationId}", correlationId);
            return StatusCode(500, new { error = "Failed to create customer portal session", correlationId });
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
            _logger.LogWarning(
                "Tenant not found for checkout session. TenantId: {TenantId}. This should not happen - tenant should be created during onboarding. CorrelationId: {CorrelationId}",
                tenantId, correlationId);
            
            // Create minimal tenant record to prevent data loss
            // In production, this should be investigated as tenants should exist before checkout
            tenant = new Data.Entities.TenantEntity
            {
                EntraIdTenantId = tenantId,
                OrganizationName = $"Tenant-{tenantId}",
                PrimaryAdminEmail = FallbackTenantEmail,
                Status = "Active",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Auto-created tenant record for {TenantId} from checkout session. CorrelationId: {CorrelationId}",
                tenantId, correlationId);
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
            subscription.StripeCustomerId = session.CustomerId; // Update customer ID if changed
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

        // The Subscription property is expandable - it's either a string ID or can be expanded to a Subscription object
        // We need to extract the ID properly
        string? subscriptionId = null;
        
        try
        {
            // Try to get subscription ID from the raw JSON data
            var jsonInvoice = stripeEvent.Data.RawObject;
            if (jsonInvoice.ContainsKey("subscription") && jsonInvoice["subscription"] != null)
            {
                subscriptionId = jsonInvoice["subscription"] as string;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract subscription ID from invoice. CorrelationId: {CorrelationId}", correlationId);
            return;
        }
        
        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogWarning("Subscription ID not found in invoice. CorrelationId: {CorrelationId}", correlationId);
            return;
        }

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

        // The Subscription property is expandable - it's either a string ID or can be expanded to a Subscription object
        // We need to extract the ID properly
        string? subscriptionId = null;
        
        try
        {
            // Try to get subscription ID from the raw JSON data
            var jsonInvoice = stripeEvent.Data.RawObject;
            if (jsonInvoice.ContainsKey("subscription") && jsonInvoice["subscription"] != null)
            {
                subscriptionId = jsonInvoice["subscription"] as string;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract subscription ID from invoice. CorrelationId: {CorrelationId}", correlationId);
            return;
        }
        
        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogWarning("Subscription ID not found in invoice. CorrelationId: {CorrelationId}", correlationId);
            return;
        }

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
