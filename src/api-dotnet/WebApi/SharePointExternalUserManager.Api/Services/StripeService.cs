using Stripe;
using Stripe.Checkout;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for interacting with Stripe API
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create a Stripe customer for a tenant
    /// </summary>
    Task<Customer> CreateCustomerAsync(string tenantId, string email, string? name = null, Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Create a checkout session for a plan
    /// </summary>
    Task<Session> CreateCheckoutSessionAsync(
        string tenantId,
        SubscriptionTier planTier,
        bool isAnnual,
        string successUrl,
        string cancelUrl);

    /// <summary>
    /// Create a customer portal session for subscription management
    /// </summary>
    Task<Stripe.BillingPortal.Session> CreateCustomerPortalSessionAsync(string customerId, string returnUrl);

    /// <summary>
    /// Update subscription to a different plan
    /// </summary>
    Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, string newPriceId);

    /// <summary>
    /// Verify webhook signature
    /// </summary>
    Event? VerifyWebhookSignature(string json, string stripeSignature);

    /// <summary>
    /// Get subscription details from Stripe
    /// </summary>
    Task<Subscription?> GetSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Cancel a subscription
    /// </summary>
    Task<Subscription> CancelSubscriptionAsync(string subscriptionId);

    /// <summary>
    /// Get customer details
    /// </summary>
    Task<Customer?> GetCustomerAsync(string customerId);

    /// <summary>
    /// Map Stripe price ID to internal plan tier
    /// </summary>
    SubscriptionTier? MapPriceToPlanTier(string priceId);
}

public class StripeService : IStripeService
{
    private readonly string _webhookSecret;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;

    // Stripe Price ID mappings (configured via environment variables)
    private readonly Dictionary<string, (SubscriptionTier Tier, bool IsAnnual)> _priceMapping;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Set Stripe API key
        var apiKey = configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Stripe:SecretKey not configured. Stripe integration will not work.");
        }
        else
        {
            StripeConfiguration.ApiKey = apiKey;
        }

        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;

        // Initialize price mappings from configuration
        _priceMapping = new Dictionary<string, (SubscriptionTier, bool)>
        {
            // Starter
            [configuration["Stripe:Price:Starter:Monthly"] ?? "price_starter_monthly"] = (SubscriptionTier.Starter, false),
            [configuration["Stripe:Price:Starter:Annual"] ?? "price_starter_annual"] = (SubscriptionTier.Starter, true),
            
            // Professional
            [configuration["Stripe:Price:Professional:Monthly"] ?? "price_professional_monthly"] = (SubscriptionTier.Professional, false),
            [configuration["Stripe:Price:Professional:Annual"] ?? "price_professional_annual"] = (SubscriptionTier.Professional, true),
            
            // Business
            [configuration["Stripe:Price:Business:Monthly"] ?? "price_business_monthly"] = (SubscriptionTier.Business, false),
            [configuration["Stripe:Price:Business:Annual"] ?? "price_business_annual"] = (SubscriptionTier.Business, true)
        };
    }

    public async Task<Customer> CreateCustomerAsync(
        string tenantId, 
        string email, 
        string? name = null, 
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            var customerMetadata = metadata ?? new Dictionary<string, string>();
            customerMetadata["tenant_id"] = tenantId;

            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = customerMetadata,
                Description = $"Tenant: {tenantId}"
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe customer {CustomerId} for tenant {TenantId}",
                customer.Id, tenantId);

            return customer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe customer for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<Stripe.BillingPortal.Session> CreateCustomerPortalSessionAsync(
        string customerId, 
        string returnUrl)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created customer portal session {SessionId} for customer {CustomerId}",
                session.Id, customerId);

            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create customer portal session for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Subscription> UpdateSubscriptionAsync(string subscriptionId, string newPriceId)
    {
        try
        {
            // Get current subscription to find the subscription item
            var currentSub = await GetSubscriptionAsync(subscriptionId);
            if (currentSub == null)
            {
                throw new InvalidOperationException($"Subscription {subscriptionId} not found");
            }

            // Get the subscription item ID (should be only one for our use case)
            var itemId = currentSub.Items.Data[0].Id;

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Id = itemId,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = "create_prorations" // Prorate the subscription change
            };

            var service = new SubscriptionService();
            var subscription = await service.UpdateAsync(subscriptionId, options);

            _logger.LogInformation(
                "Updated subscription {SubscriptionId} to price {PriceId}",
                subscriptionId, newPriceId);

            return subscription;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to update subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Session> CreateCheckoutSessionAsync(
        string tenantId,
        SubscriptionTier planTier,
        bool isAnnual,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            // Get the appropriate price ID
            var priceId = GetPriceId(planTier, isAnnual);

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = tenantId, // Store tenant ID for later reference
                Metadata = new Dictionary<string, string>
                {
                    { "tenant_id", tenantId },
                    { "plan_tier", planTier.ToString() }
                },
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "tenant_id", tenantId },
                        { "plan_tier", planTier.ToString() }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Stripe checkout session {SessionId} for tenant {TenantId} plan {PlanTier}",
                session.Id, tenantId, planTier);

            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout session for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public Event? VerifyWebhookSignature(string json, string stripeSignature)
    {
        try
        {
            if (string.IsNullOrEmpty(_webhookSecret))
            {
                _logger.LogWarning("Webhook secret not configured. Cannot verify webhook signature.");
                return null;
            }

            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _webhookSecret,
                throwOnApiVersionMismatch: false
            );

            _logger.LogInformation("Verified webhook signature for event {EventId} type {EventType}",
                stripeEvent.Id, stripeEvent.Type);

            return stripeEvent;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to verify webhook signature");
            return null;
        }
    }

    public async Task<Subscription?> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            return await service.GetAsync(subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to retrieve subscription {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var service = new SubscriptionService();
            return await service.CancelAsync(subscriptionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId}", subscriptionId);
            throw;
        }
    }

    public async Task<Customer?> GetCustomerAsync(string customerId)
    {
        try
        {
            var service = new CustomerService();
            return await service.GetAsync(customerId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to retrieve customer {CustomerId}", customerId);
            return null;
        }
    }

    public SubscriptionTier? MapPriceToPlanTier(string priceId)
    {
        if (_priceMapping.TryGetValue(priceId, out var mapping))
        {
            return mapping.Tier;
        }
        
        _logger.LogWarning("Unknown Stripe price ID: {PriceId}", priceId);
        return null;
    }

    private string GetPriceId(SubscriptionTier planTier, bool isAnnual)
    {
        var configKey = $"Stripe:Price:{planTier}:{(isAnnual ? "Annual" : "Monthly")}";
        var priceId = _configuration[configKey];

        if (string.IsNullOrEmpty(priceId))
        {
            var errorMsg = $"Price ID not configured for {planTier} {(isAnnual ? "annual" : "monthly")}. " +
                          $"Please set {configKey} in configuration.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        return priceId;
    }
}
