#!/bin/bash

# SharePoint External User Manager - Dev Environment Deployment Script
# This script deploys the complete SaaS platform to Azure

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="${ENVIRONMENT:-dev}"
RESOURCE_GROUP="${RESOURCE_GROUP:-spexternal-$ENVIRONMENT-rg}"
LOCATION="${LOCATION:-uksouth}"
APP_NAME="${APP_NAME:-spexternal}"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}SharePoint External User Manager${NC}"
echo -e "${BLUE}Azure Deployment Script${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Function to print status messages
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it first:"
    echo "  https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in to Azure
print_status "Checking Azure login status..."
if ! az account show &> /dev/null; then
    print_error "You are not logged in to Azure. Please run 'az login' first."
    exit 1
fi

SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
print_success "Logged in to Azure subscription: $SUBSCRIPTION_NAME"

# Prompt for SQL credentials if not set
if [ -z "$SQL_ADMIN_USERNAME" ]; then
    read -p "Enter SQL Admin Username (default: sqladmin): " SQL_ADMIN_USERNAME
    SQL_ADMIN_USERNAME="${SQL_ADMIN_USERNAME:-sqladmin}"
fi

if [ -z "$SQL_ADMIN_PASSWORD" ]; then
    read -s -p "Enter SQL Admin Password (min 8 chars, must have uppercase, lowercase, numbers, special chars): " SQL_ADMIN_PASSWORD
    echo ""
    
    # Basic password validation
    if [ ${#SQL_ADMIN_PASSWORD} -lt 8 ]; then
        print_error "Password must be at least 8 characters long"
        exit 1
    fi
fi

echo ""
print_status "Deployment Configuration:"
echo "  Environment:      $ENVIRONMENT"
echo "  Resource Group:   $RESOURCE_GROUP"
echo "  Location:         $LOCATION"
echo "  App Name:         $APP_NAME"
echo ""

# Confirm deployment
read -p "Continue with deployment? (y/n): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    print_warning "Deployment cancelled"
    exit 0
fi

# Step 1: Create Resource Group
print_status "Creating resource group..."
if az group create --name "$RESOURCE_GROUP" --location "$LOCATION" &> /dev/null; then
    print_success "Resource group created or already exists"
else
    print_error "Failed to create resource group"
    exit 1
fi

# Step 2: Deploy Infrastructure
print_status "Deploying Azure infrastructure (this may take 10-15 minutes)..."
DEPLOYMENT_NAME="main-$(date +%Y%m%d-%H%M%S)"

if az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infra/bicep/main.bicep \
    --parameters environment="$ENVIRONMENT" \
    --parameters sqlAdminUsername="$SQL_ADMIN_USERNAME" \
    --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
    --name "$DEPLOYMENT_NAME" \
    --output none; then
    print_success "Infrastructure deployed successfully"
else
    print_error "Infrastructure deployment failed"
    exit 1
fi

# Step 3: Get deployment outputs
print_status "Retrieving deployment outputs..."
API_APP_NAME=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query 'properties.outputs.apiAppName.value' -o tsv)

PORTAL_APP_NAME=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query 'properties.outputs.portalAppName.value' -o tsv)

API_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query 'properties.outputs.apiAppUrl.value' -o tsv)

PORTAL_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query 'properties.outputs.portalAppUrl.value' -o tsv)

KEY_VAULT_NAME=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$DEPLOYMENT_NAME" \
    --query 'properties.outputs.keyVaultName.value' -o tsv)

print_success "Retrieved deployment information"

# Step 4: Build and Deploy API
print_status "Building API..."
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

if dotnet restore && \
   dotnet build --configuration Release --no-restore && \
   dotnet publish --configuration Release --output ./publish --no-build; then
    print_success "API built successfully"
else
    print_error "API build failed"
    exit 1
fi

print_status "Deploying API to Azure App Service..."
cd publish
zip -q -r ../api-deploy.zip .
cd ..

if az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$API_APP_NAME" \
    --src api-deploy.zip \
    --output none; then
    print_success "API deployed successfully"
else
    print_error "API deployment failed"
    exit 1
fi

cd ../../../../..

# Step 5: Build and Deploy Blazor Portal
print_status "Building Blazor Portal..."
cd src/portal-blazor/SharePointExternalUserManager.Portal

if dotnet restore && \
   dotnet build --configuration Release --no-restore && \
   dotnet publish --configuration Release --output ./publish --no-build; then
    print_success "Blazor Portal built successfully"
else
    print_error "Blazor Portal build failed"
    exit 1
fi

print_status "Deploying Blazor Portal to Azure App Service..."
cd publish
zip -q -r ../portal-deploy.zip .
cd ..

if az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$PORTAL_APP_NAME" \
    --src portal-deploy.zip \
    --output none; then
    print_success "Blazor Portal deployed successfully"
else
    print_error "Blazor Portal deployment failed"
    exit 1
fi

cd ../../..

# Step 6: Build SPFx Package
print_status "Building SPFx Client package..."
cd src/client-spfx

if [ -f "package-lock.json" ]; then
    if npm ci --no-optional --legacy-peer-deps --silent && \
       npm run build --silent && \
       npm run package-solution --silent; then
        print_success "SPFx package built successfully"
        SPPKG_FILE=$(ls sharepoint/solution/*.sppkg 2>/dev/null | head -n 1)
        if [ -n "$SPPKG_FILE" ]; then
            print_success "SPFx package: $SPPKG_FILE"
        fi
    else
        print_warning "SPFx build failed (this is optional)"
    fi
else
    print_warning "SPFx dependencies not installed. Run 'npm install' in src/client-spfx"
fi

cd ../..

# Step 7: Summary
echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}Deployment Completed Successfully!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "Environment:         $ENVIRONMENT"
echo "Resource Group:      $RESOURCE_GROUP"
echo ""
echo "API App Service:     $API_APP_NAME"
echo "API URL:             $API_URL"
echo ""
echo "Portal App Service:  $PORTAL_APP_NAME"
echo "Portal URL:          $PORTAL_URL"
echo ""
echo "Key Vault:           $KEY_VAULT_NAME"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Configure Entra ID app registrations"
echo "2. Add secrets to Key Vault:"
echo "   - StripeApiKey"
echo "   - GraphClientSecret"
echo "3. Run database migrations"
echo "4. Configure App Service settings"
echo "5. Deploy SPFx package to SharePoint App Catalog"
echo ""
echo "For detailed post-deployment steps, see:"
echo "  docs/DEPLOYMENT.md"
echo ""
echo -e "${GREEN}Deployment logs and outputs have been saved${NC}"
echo ""

# Save deployment information to file
cat > deployment-info-$ENVIRONMENT.txt << EOF
Deployment Information - $(date)
====================================
Environment:         $ENVIRONMENT
Resource Group:      $RESOURCE_GROUP
Location:            $LOCATION
Deployment Name:     $DEPLOYMENT_NAME

API App Service:     $API_APP_NAME
API URL:             $API_URL

Portal App Service:  $PORTAL_APP_NAME
Portal URL:          $PORTAL_URL

Key Vault:           $KEY_VAULT_NAME
====================================
EOF

print_success "Deployment information saved to: deployment-info-$ENVIRONMENT.txt"
