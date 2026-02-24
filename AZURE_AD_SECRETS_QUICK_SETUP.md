# Quick Setup: Azure AD Repository Secrets

## ⚡ Quick Reference

This is a quick checklist to configure the required Azure AD secrets for automated deployments.

## ✅ Action Items

### 1. Get Your Azure AD Values

Go to [Azure Portal](https://portal.azure.com) → **Azure Active Directory** → **App registrations** → Select your app:

- [ ] Copy **Application (client) ID**: _____________________________________
- [ ] Copy **Directory (tenant) ID**: _____________________________________
- [ ] Go to **Certificates & secrets** → **New client secret** → Copy **Value**: _____________________________________

### 2. Add to GitHub Repository

Go to your repository → **Settings** → **Secrets and variables** → **Actions**:

- [ ] Add secret `AZURE_AD_CLIENT_ID` with the Application (client) ID
- [ ] Add secret `AZURE_AD_CLIENT_SECRET` with the Client Secret value
- [ ] Add secret `AZURE_AD_TENANT_ID` with the Directory (tenant) ID

### 3. Verify Setup

- [ ] All three secrets are visible in the repository secrets page (masked)
- [ ] No errors in the secrets configuration
- [ ] Ready to trigger a deployment

## 🎯 Expected Values

Based on your current appsettings.json:

| Secret Name | Current Value in appsettings.json | What to Use |
|-------------|-----------------------------------|-------------|
| `AZURE_AD_CLIENT_ID` | `61def48e-a9bc-43ef-932b-10eabef14c2a` | Use the same or update if needed |
| `AZURE_AD_TENANT_ID` | `b884f3d2-f3d0-4e67-8470-bc7b0372ebb6` | Use the same or update if needed |
| `AZURE_AD_CLIENT_SECRET` | (empty) | Get from Azure Portal → Certificates & secrets |

## 🚀 After Setup

Once secrets are configured:

1. Push to `main` branch or manually trigger the workflow
2. The workflow will:
   - ✅ Validate secrets are configured
   - ✅ Create appsettings.Production.json with your secrets
   - ✅ Build and deploy the application
   - ✅ Application starts successfully with Azure AD authentication

## 📖 Need More Help?

- **Detailed Guide**: [AZURE_AD_SECRETS_SETUP.md](./AZURE_AD_SECRETS_SETUP.md)
- **All Secrets**: [WORKFLOW_SECRET_SETUP.md](./WORKFLOW_SECRET_SETUP.md)
- **Configuration**: [CONFIGURATION_GUIDE.md](./CONFIGURATION_GUIDE.md)

## ⚠️ Common Mistakes

- ❌ Using the Secret ID instead of the Secret Value (use the Value!)
- ❌ Not creating a client secret in Azure Portal first
- ❌ Misspelling the secret names (they must match exactly)
- ❌ Using expired client secrets (check expiration date)
- ❌ Using placeholders or empty values
