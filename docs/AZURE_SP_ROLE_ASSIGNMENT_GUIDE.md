# Azure Service Principal Role Assignment Guide

## Context

The `azure-infra-setup.yml` workflow authenticates to Azure using **OIDC federated credentials** (no client secret stored in GitHub). The service principal associated with client ID `c7ba4621-ef47-4bbe-9497-4b96ce1b926e` must hold the **Contributor** role on the target subscription or resource group.

Without this role assignment the workflow will fail with:

```
ERROR: (AuthorizationFailed) The client '…' with object id '…' does not have authorization
to perform action … over scope … or the scope is invalid.
Code: AuthorizationFailed  HTTP: 403
```

---

## Table of Contents

1. [Step 1 — Confirm the service principal exists](#step-1--confirm-the-service-principal-exists)
2. [Step 2 — Assign Contributor role via Azure CLI](#step-2--assign-contributor-role-via-azure-cli)
3. [Step 3 — Assign Contributor role via Azure Portal (when SP is not visible in search)](#step-3--assign-contributor-role-via-azure-portal-when-sp-is-not-visible-in-search)
4. [Step 4 — If the service principal cannot be found](#step-4--if-the-service-principal-cannot-be-found)
5. [Required GitHub Secrets for the workflow](#required-github-secrets-for-the-workflow)
6. [Verify the workflow works](#verify-the-workflow-works)

---

## Step 1 — Confirm the service principal exists

Run the following Azure CLI commands. To **read** service principal data you need Azure AD *Reader* (or higher). To **assign roles** you additionally need the *Owner* or *User Access Administrator* RBAC role on the target subscription or resource group.

```bash
# Log in to Azure
az login

# Option A: look up by application (client) ID
az ad sp show --id c7ba4621-ef47-4bbe-9497-4b96ce1b926e

# Option B: search by display name if you know it
az ad sp list --display-name "sharepoint-external-user-manager" --output table

# Option C: list all SPs and grep (use sparingly on large tenants)
az ad sp list --all --query "[?appId=='c7ba4621-ef47-4bbe-9497-4b96ce1b926e']" --output json
```

**Expected output** (abbreviated):

```json
{
  "appId": "c7ba4621-ef47-4bbe-9497-4b96ce1b926e",
  "displayName": "sharepoint-external-user-manager",
  "id": "<object-id-guid>",
  "accountEnabled": true,
  "servicePrincipalType": "Application"
}
```

Note the `id` field (also called the **object ID**). You will need it for the CLI role assignment.

### Checking for disabled or soft-deleted SPs

```bash
# Check if it is disabled
az ad sp show --id c7ba4621-ef47-4bbe-9497-4b96ce1b926e \
  --query "{displayName:displayName,enabled:accountEnabled,id:id}" --output json

# List soft-deleted service principals
# Requires: Azure AD P1/P2 tenant licence AND the user must have
# Global Administrator or Privileged Role Administrator role
az rest --method GET \
  --url "https://graph.microsoft.com/v1.0/directory/deletedItems/microsoft.graph.servicePrincipal" \
  --query "value[?appId=='c7ba4621-ef47-4bbe-9497-4b96ce1b926e']" --output json
```

If `accountEnabled` is `false`, re-enable it:

```bash
az ad sp update --id c7ba4621-ef47-4bbe-9497-4b96ce1b926e --set accountEnabled=true
```

---

## Step 2 — Assign Contributor role via Azure CLI

Replace the placeholders with your values.

```bash
# Variables
CLIENT_ID="c7ba4621-ef47-4bbe-9497-4b96ce1b926e"
SUBSCRIPTION_ID="<your-subscription-id>"          # az account show --query id -o tsv
RESOURCE_GROUP="spexternal-dev-rg"                 # omit --scope flag to assign at subscription level

# Retrieve the service principal object ID
SP_OBJECT_ID=$(az ad sp show --id "$CLIENT_ID" --query id --output tsv)
echo "Service principal object ID: $SP_OBJECT_ID"

# Option A — Assign Contributor at resource group scope (recommended, least privilege)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP"

# Option B — Assign Contributor at subscription scope (broader, use only if you need
#             to create resource groups via the workflow)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"
```

> **Tip:** Use `--assignee-object-id` together with `--assignee-principal-type ServicePrincipal`
> instead of `--assignee "$CLIENT_ID"`. This avoids the internal Graph lookup that can
> fail when the SP is not yet visible in the Portal search index.

Verify the assignment:

```bash
az role assignment list \
  --assignee "$SP_OBJECT_ID" \
  --scope "/subscriptions/$SUBSCRIPTION_ID" \
  --query "[].{role:roleDefinitionName,scope:scope}" \
  --output table
```

---

## Step 3 — Assign Contributor role via Azure Portal (when SP is not visible in search)

The Azure Portal IAM "Add role assignment" dialog searches Azure AD by **display name or UPN**. A service principal that was recently created, renamed, or has a non-standard name may **not appear** in the type-ahead results. Use the workaround below.

### Workaround A — Use the object ID directly in the Portal

1. In Azure Portal, navigate to your **Subscription** (or Resource Group) → **Access control (IAM)**.
2. Click **+ Add → Add role assignment**.
3. Select the **Contributor** role → click **Next**.
4. Under **Assign access to**, choose **User, group, or service principal**.
5. Click **+ Select members**.
6. In the search box, paste the **object ID** (not the client/app ID) of the service principal:
   ```
   <object-id obtained in Step 1>
   ```
   If it still does not appear, paste the **display name** exactly (case-sensitive).
7. If neither match works, click **Cancel**, and use the CLI method in Step 2 instead.

### Workaround B — Use the Portal "Members" tab with object ID filter

1. In the IAM blade, go to the **Role assignments** tab.
2. Click **Add → Add role assignment**.
3. Choose **Privileged administrator roles** → **Contributor** → **Next**.
4. Under **Members**, click **+ Select members**.
5. In the right-hand panel search field, enter the full object ID.

### Why the SP may not appear in Portal search

| Reason | Fix |
|--------|-----|
| Recently created (search index lag, up to 15 min) | Wait and retry |
| Display name contains special characters | Search by object ID |
| SP was created in a different directory | Confirm you are in the correct tenant |
| SP is a **multi-tenant** application's SP | Search the home tenant first, then the resource tenant |

---

## Step 4 — If the service principal cannot be found

If `az ad sp show --id c7ba4621-ef47-4bbe-9497-4b96ce1b926e` returns a `Resource 'c7ba4621…' does not exist` error, the service principal has been deleted and must be recreated.

> **Note:** You do NOT need to create a new App Registration manually. The OIDC federated
> credential is set up via the GitHub repository settings and Azure AD. Follow the steps below.

### Re-create the OIDC federated credential

#### Azure Portal path

1. Go to **Azure Portal → Azure Active Directory → App registrations**.
2. Search for the app by name (e.g., `sharepoint-external-user-manager`).
   - If the app registration is also gone, click **+ New registration**:
     - Name: `sharepoint-external-user-manager`
     - Supported account types: Single tenant
     - Click **Register**
3. Note the new **Application (client) ID**.
4. Go to **Certificates & secrets → Federated credentials → + Add credential**.
5. Choose **GitHub Actions deploying Azure resources**.
6. Fill in:
   - **Organization**: `orkinosai25-org`
   - **Repository**: `sharepoint-external-user-manager`
   - **Entity type**: `Branch`
   - **Branch**: `main` (repeat for other branches as needed)
   - **Name**: e.g., `github-actions-main`
7. Click **Add**.
8. Go to **Enterprise applications → sharepoint-external-user-manager → Properties** and confirm **Enabled for users to sign-in** is `Yes` / `accountEnabled` is `true`.
9. Assign the Contributor role as described in Step 2 using the new object ID.
10. Update the GitHub secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` with the new values (see [Required GitHub Secrets](#required-github-secrets-for-the-workflow)).

#### Azure CLI path

```bash
# 1. Create new app registration
APP=$(az ad app create --display-name "sharepoint-external-user-manager" --output json)
APP_ID=$(echo "$APP" | jq -r '.appId')
echo "New app ID: $APP_ID"

# 2. Create service principal for the app
SP=$(az ad sp create --id "$APP_ID" --output json)
SP_OBJECT_ID=$(echo "$SP" | jq -r '.id')
echo "New SP object ID: $SP_OBJECT_ID"

# 3. Add federated credential for GitHub Actions (branch: main)
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters '{
    "name": "github-actions-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:orkinosai25-org/sharepoint-external-user-manager:ref:refs/heads/main",
    "description": "GitHub Actions OIDC for main branch",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# 4. (Optional) Add federated credential for workflow_dispatch from any branch
az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters '{
    "name": "github-actions-dispatch",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:orkinosai25-org/sharepoint-external-user-manager:environment:dev",
    "description": "GitHub Actions OIDC for dev environment",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# 5. Assign Contributor role at resource group scope
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
az role assignment create \
  --assignee-object-id "$SP_OBJECT_ID" \
  --assignee-principal-type ServicePrincipal \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/spexternal-dev-rg"

echo ""
echo "Update GitHub secrets with:"
echo "  AZURE_CLIENT_ID      = $APP_ID"
echo "  AZURE_TENANT_ID      = $(az account show --query tenantId --output tsv)"
echo "  AZURE_SUBSCRIPTION_ID= $SUBSCRIPTION_ID"
```

---

## Required GitHub Secrets for the workflow

Go to **repository → Settings → Secrets and variables → Actions** and add:

| Secret name | Description | How to find |
|-------------|-------------|-------------|
| `AZURE_CLIENT_ID` | Application (client) ID of the service principal | Azure Portal → App registrations → your app → Overview |
| `AZURE_TENANT_ID` | Azure AD tenant ID | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `az account show --query id -o tsv` |
| `SQL_ADMIN_PASSWORD` | SQL Server admin password (strong, min 12 chars) | Choose a secure password and store it here |

> The workflow uses OIDC — **no** `AZURE_CLIENT_SECRET` is needed.

---

## Verify the workflow works

1. Push or trigger `azure-infra-setup.yml` with **validate_only = true** first.
2. Confirm the "Azure Login (OIDC)" step succeeds (no 401/403).
3. If validation passes, re-run with **validate_only = false** to provision resources.

### Quick CLI smoke-test before running the workflow

```bash
# Log in as the service principal using OIDC is not possible locally,
# but you can verify the role assignment is in place:
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
CLIENT_ID="c7ba4621-ef47-4bbe-9497-4b96ce1b926e"
SP_OID=$(az ad sp show --id "$CLIENT_ID" --query id --output tsv)

az role assignment list \
  --assignee "$SP_OID" \
  --scope "/subscriptions/$SUBSCRIPTION_ID" \
  --query "[?roleDefinitionName=='Contributor'].{role:roleDefinitionName,scope:scope}" \
  --output table
```

Expected output:

```
Role          Scope
------------  ---------------------------------------------------
Contributor   /subscriptions/<subscription-id>
```

If the table is empty, the role has not been assigned yet — go back to Step 2.
