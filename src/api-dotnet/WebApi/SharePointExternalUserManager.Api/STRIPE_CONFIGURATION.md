# Stripe Configuration Guide

## Overview

This guide explains how to configure Stripe billing for local development and production deployment.

## ⚠️ Security Notice

**NEVER commit secrets to source control!**

The Stripe configuration has been removed from `appsettings.Development.json` to prevent accidental secret exposure. Follow the instructions below to configure your development environment.

## Local Development Setup

### Option 1: User Secrets (Recommended)

Use .NET User Secrets to store your Stripe configuration securely on your local machine:

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

# Set Stripe API keys
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_SECRET_KEY"
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_PUBLISHABLE_KEY"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_WEBHOOK_SECRET"

# Set Stripe price IDs
dotnet user-secrets set "Stripe:Price:Starter:Monthly" "price_YOUR_STARTER_MONTHLY_ID"
dotnet user-secrets set "Stripe:Price:Starter:Annual" "price_YOUR_STARTER_ANNUAL_ID"
dotnet user-secrets set "Stripe:Price:Professional:Monthly" "price_YOUR_PROFESSIONAL_MONTHLY_ID"
dotnet user-secrets set "Stripe:Price:Professional:Annual" "price_YOUR_PROFESSIONAL_ANNUAL_ID"
dotnet user-secrets set "Stripe:Price:Business:Monthly" "price_YOUR_BUSINESS_MONTHLY_ID"
dotnet user-secrets set "Stripe:Price:Business:Annual" "price_YOUR_BUSINESS_ANNUAL_ID"
```

### Option 2: Environment Variables

Set environment variables in your shell or IDE:

**Windows (PowerShell):**
```powershell
$env:Stripe__SecretKey="sk_test_YOUR_SECRET_KEY"
$env:Stripe__PublishableKey="pk_test_YOUR_PUBLISHABLE_KEY"
$env:Stripe__WebhookSecret="whsec_YOUR_WEBHOOK_SECRET"
$env:Stripe__Price__Starter__Monthly="price_YOUR_STARTER_MONTHLY_ID"
# ... etc
```

**Linux/macOS (Bash):**
```bash
export Stripe__SecretKey="sk_test_YOUR_SECRET_KEY"
export Stripe__PublishableKey="pk_test_YOUR_PUBLISHABLE_KEY"
export Stripe__WebhookSecret="whsec_YOUR_WEBHOOK_SECRET"
export Stripe__Price__Starter__Monthly="price_YOUR_STARTER_MONTHLY_ID"
# ... etc
```

**Note:** In environment variables, use double underscores (`__`) instead of colons (`:`) to represent nested configuration.

### Option 3: Local Configuration File (Not Recommended)

If you must use a local file, copy `appsettings.Stripe.example.json`:

```bash
cp appsettings.Stripe.example.json appsettings.Stripe.json
```

Then edit `appsettings.Stripe.json` with your actual values.

**Important:** Make sure `appsettings.Stripe.json` is in your `.gitignore`!

## Getting Stripe Keys

### 1. API Keys

1. Log in to [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers → API Keys**
3. Copy your **Secret key** (starts with `sk_test_...` for test mode)
4. Copy your **Publishable key** (starts with `pk_test_...` for test mode)

### 2. Webhook Secret

1. Navigate to **Developers → Webhooks**
2. Click **Add endpoint**
3. Enter your webhook URL: `https://your-api.azurewebsites.net/api/billing/webhook`
4. Select events to listen to:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.paid`
   - `invoice.payment_failed`
5. Click **Add endpoint**
6. Click on your new endpoint
7. Click **Reveal** next to **Signing secret**
8. Copy the webhook secret (starts with `whsec_...`)

### 3. Price IDs

For each plan tier (Starter, Professional, Business), create products and prices:

1. Navigate to **Products** in Stripe Dashboard
2. Click **Add product**
3. Fill in product details:
   - **Name:** SharePoint External User Manager - [Tier Name]
   - **Description:** [Plan description]
4. Add two **Recurring** prices:
   - **Monthly:** Set your monthly price, billing period = Monthly
   - **Annual:** Set your annual price, billing period = Yearly
5. After saving, copy each **Price ID** (starts with `price_...`)
6. Configure both monthly and annual price IDs for each tier

## Configuration Structure

Your complete Stripe configuration should look like this:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "Price": {
      "Starter": {
        "Monthly": "price_...",
        "Annual": "price_..."
      },
      "Professional": {
        "Monthly": "price_...",
        "Annual": "price_..."
      },
      "Business": {
        "Monthly": "price_...",
        "Annual": "price_..."
      }
    }
  }
}
```

## Testing Your Configuration

Run the API and test the plans endpoint:

```bash
dotnet run

# In another terminal:
curl https://localhost:7001/api/billing/plans
```

You should see a list of all available plans.

## Production Deployment

### Azure App Service

1. Navigate to your App Service in Azure Portal
2. Go to **Configuration** → **Application settings**
3. Add each Stripe configuration value as a separate application setting:
   - `Stripe__SecretKey` = `sk_live_...` (use live keys!)
   - `Stripe__PublishableKey` = `pk_live_...`
   - `Stripe__WebhookSecret` = `whsec_...` (from production webhook)
   - `Stripe__Price__Starter__Monthly` = `price_...`
   - etc.
4. Click **Save**

**Important:** 
- Use **live keys** (`sk_live_...`, `pk_live_...`) in production
- Create a separate production webhook endpoint
- Use production price IDs (not test prices)

### Azure Key Vault (Best Practice)

For enhanced security, store secrets in Azure Key Vault:

1. Create an Azure Key Vault
2. Enable managed identity for your App Service
3. Grant App Service access to Key Vault
4. Store secrets in Key Vault
5. Reference Key Vault secrets in App Service configuration:
   ```
   @Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/Stripe-SecretKey/)
   ```

## Troubleshooting

### "Price ID not configured" error

**Problem:** API throws exception when creating checkout session.

**Solution:** Verify all price IDs are configured. Check the configuration key names match exactly.

### Webhook signature verification failed

**Problem:** Webhooks return 400 Bad Request.

**Solution:** 
- Verify webhook secret is correct
- Ensure you copied the signing secret, not the endpoint ID
- Check you're using the correct webhook (test vs. production)

### Test mode vs. Live mode mismatch

**Problem:** API calls fail with authentication errors.

**Solution:** Ensure all your keys are from the same mode:
- Test keys (`sk_test_...`) must be used with test price IDs
- Live keys (`sk_live_...`) must be used with live price IDs
- Never mix test and live keys

## Security Checklist

- [ ] No secrets committed to source control
- [ ] Using user secrets or environment variables for local development
- [ ] Production secrets stored in Azure Key Vault or App Service application settings
- [ ] Different keys for dev/staging/production environments
- [ ] Webhook secret configured correctly
- [ ] API keys rotated every 90 days
- [ ] Monitoring enabled for failed webhook deliveries

## See Also

- [Stripe Setup Documentation](../STRIPE_SETUP.md)
- [Stripe Implementation Guide](../STRIPE_IMPLEMENTATION.md)
- [Stripe API Documentation](https://stripe.com/docs/api)
- [.NET User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
