#!/usr/bin/env bash
# =============================================================================
# setup-azure-openai.sh
# Automates provisioning of an Azure OpenAI resource and GPT-4 deployment,
# then writes the required settings into the API appsettings.json and (for
# App Service deployments) into the App Service application settings.
#
# Prerequisites:
#   - Azure CLI installed and logged in  (az login)
#   - jq installed                       (brew install jq / apt install jq)
#   - Sufficient RBAC permissions:
#       Contributor on the target resource group AND
#       "Cognitive Services OpenAI Contributor" on the subscription
#
# Usage:
#   chmod +x scripts/setup-azure-openai.sh
#   ./scripts/setup-azure-openai.sh [OPTIONS]
#
# Options (all optional – you will be prompted for any that are missing):
#   --subscription  <id>        Azure subscription ID
#   --resource-group <name>     Resource group name          (default: spexternal-rg)
#   --location <region>         Azure region                 (default: eastus)
#   --openai-name <name>        Azure OpenAI resource name   (default: spexternal-openai)
#   --deployment-name <name>    Model deployment name        (default: gpt-4)
#   --model <name>              Model name                   (default: gpt-4)
#   --app-service-name <name>   App Service name (optional) – if set, app settings
#                               are pushed directly to the App Service
#   --appsettings-file <path>   Path to the API appsettings.json to update
#                               (default: src/api-dotnet/WebApi/SharePointExternalUserManager.Api/appsettings.json)
# =============================================================================
set -euo pipefail

# ── helpers ──────────────────────────────────────────────────────────────────
info()  { echo -e "\033[1;32m[INFO]\033[0m  $*"; }
warn()  { echo -e "\033[1;33m[WARN]\033[0m  $*"; }
error() { echo -e "\033[1;31m[ERROR]\033[0m $*" >&2; exit 1; }

prompt_if_empty() {
  local var_name="$1"
  local prompt_text="$2"
  local default_val="${3:-}"
  if [[ -z "${!var_name:-}" ]]; then
    if [[ -n "$default_val" ]]; then
      read -rp "$prompt_text [$default_val]: " input
      eval "$var_name=\"${input:-$default_val}\""
    else
      read -rp "$prompt_text: " input
      [[ -z "$input" ]] && error "$var_name is required."
      eval "$var_name=\"$input\""
    fi
  fi
}

# ── defaults ─────────────────────────────────────────────────────────────────
SUBSCRIPTION=""
RESOURCE_GROUP="spexternal-rg"
LOCATION="eastus"
OPENAI_NAME="spexternal-openai"
DEPLOYMENT_NAME="gpt-4"
MODEL_NAME="gpt-4"
APP_SERVICE_NAME=""
APPSETTINGS_FILE="src/api-dotnet/WebApi/SharePointExternalUserManager.Api/appsettings.json"

# ── parse flags ───────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --subscription)      SUBSCRIPTION="$2";      shift 2 ;;
    --resource-group)    RESOURCE_GROUP="$2";     shift 2 ;;
    --location)          LOCATION="$2";           shift 2 ;;
    --openai-name)       OPENAI_NAME="$2";        shift 2 ;;
    --deployment-name)   DEPLOYMENT_NAME="$2";    shift 2 ;;
    --model)             MODEL_NAME="$2";         shift 2 ;;
    --app-service-name)  APP_SERVICE_NAME="$2";   shift 2 ;;
    --appsettings-file)  APPSETTINGS_FILE="$2";   shift 2 ;;
    *) error "Unknown option: $1" ;;
  esac
done

# ── validate tools ────────────────────────────────────────────────────────────
command -v az  >/dev/null 2>&1 || error "Azure CLI (az) is not installed. See https://aka.ms/install-azure-cli"
command -v jq  >/dev/null 2>&1 || error "jq is not installed. Run: brew install jq  OR  apt install jq"

# ── interactive prompts ───────────────────────────────────────────────────────
prompt_if_empty SUBSCRIPTION    "Azure Subscription ID"
prompt_if_empty RESOURCE_GROUP  "Resource group name"         "$RESOURCE_GROUP"
prompt_if_empty LOCATION        "Azure region"                "$LOCATION"
prompt_if_empty OPENAI_NAME     "Azure OpenAI resource name"  "$OPENAI_NAME"
prompt_if_empty DEPLOYMENT_NAME "Model deployment name"       "$DEPLOYMENT_NAME"
prompt_if_empty MODEL_NAME      "Model name"                  "$MODEL_NAME"

# ── set active subscription ───────────────────────────────────────────────────
info "Setting active subscription to: $SUBSCRIPTION"
az account set --subscription "$SUBSCRIPTION"

# ── ensure resource group exists ─────────────────────────────────────────────
if ! az group show --name "$RESOURCE_GROUP" &>/dev/null; then
  info "Creating resource group '$RESOURCE_GROUP' in '$LOCATION'…"
  az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
  info "Resource group '$RESOURCE_GROUP' already exists."
fi

# ── create Azure OpenAI resource ─────────────────────────────────────────────
if az cognitiveservices account show \
     --name "$OPENAI_NAME" \
     --resource-group "$RESOURCE_GROUP" &>/dev/null; then
  info "Azure OpenAI resource '$OPENAI_NAME' already exists – skipping creation."
else
  info "Creating Azure OpenAI resource '$OPENAI_NAME'…"
  az cognitiveservices account create \
    --name "$OPENAI_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --kind OpenAI \
    --sku S0 \
    --yes
fi

# ── retrieve endpoint ─────────────────────────────────────────────────────────
ENDPOINT=$(az cognitiveservices account show \
  --name "$OPENAI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.endpoint" -o tsv)
info "Endpoint: $ENDPOINT"

# ── retrieve API key ──────────────────────────────────────────────────────────
API_KEY=$(az cognitiveservices account keys list \
  --name "$OPENAI_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "key1" -o tsv)

# ── deploy model ──────────────────────────────────────────────────────────────
if az cognitiveservices account deployment show \
     --name "$OPENAI_NAME" \
     --resource-group "$RESOURCE_GROUP" \
     --deployment-name "$DEPLOYMENT_NAME" &>/dev/null; then
  info "Model deployment '$DEPLOYMENT_NAME' already exists – skipping."
else
  info "Deploying model '$MODEL_NAME' as '$DEPLOYMENT_NAME'…"
  az cognitiveservices account deployment create \
    --name "$OPENAI_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --deployment-name "$DEPLOYMENT_NAME" \
    --model-name "$MODEL_NAME" \
    --model-version "0613" \
    --model-format OpenAI \
    --sku-name "Standard" \
    --sku-capacity 1
fi

# ── update appsettings.json ───────────────────────────────────────────────────
if [[ -f "$APPSETTINGS_FILE" ]]; then
  info "Updating $APPSETTINGS_FILE with Azure OpenAI settings…"
  TEMP_FILE=$(mktemp)
  jq --arg endpoint "$ENDPOINT" \
     --arg apiKey   "$API_KEY" \
     --arg deploy   "$DEPLOYMENT_NAME" \
     --arg model    "$MODEL_NAME" \
     '.AzureOpenAI.Endpoint       = $endpoint  |
      .AzureOpenAI.ApiKey         = $apiKey    |
      .AzureOpenAI.DeploymentName = $deploy    |
      .AzureOpenAI.Model          = $model     |
      .AzureOpenAI.UseDemoMode    = false' \
     "$APPSETTINGS_FILE" > "$TEMP_FILE"
  mv "$TEMP_FILE" "$APPSETTINGS_FILE"
  warn "⚠️  appsettings.json now contains the API key – DO NOT commit this file."
  warn "   Add it to .gitignore or use environment variables / Azure Key Vault in CI/CD."
else
  warn "appsettings.json not found at '$APPSETTINGS_FILE'. Skipping file update."
fi

# ── push to App Service (optional) ───────────────────────────────────────────
if [[ -n "$APP_SERVICE_NAME" ]]; then
  info "Pushing settings to App Service '$APP_SERVICE_NAME'…"
  az webapp config appsettings set \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --settings \
      "AzureOpenAI__Endpoint=$ENDPOINT" \
      "AzureOpenAI__ApiKey=$API_KEY" \
      "AzureOpenAI__DeploymentName=$DEPLOYMENT_NAME" \
      "AzureOpenAI__Model=$MODEL_NAME" \
      "AzureOpenAI__UseDemoMode=false"
  info "App Service settings updated successfully."
fi

# ── summary ───────────────────────────────────────────────────────────────────
echo ""
info "✅ Azure OpenAI setup complete."
echo ""
echo "  Resource name : $OPENAI_NAME"
echo "  Resource group: $RESOURCE_GROUP"
echo "  Endpoint      : $ENDPOINT"
echo "  Deployment    : $DEPLOYMENT_NAME  (model: $MODEL_NAME)"
echo ""
echo "  Add the following environment variables to your deployment environment:"
echo "    AzureOpenAI__Endpoint=$ENDPOINT"
echo "    AzureOpenAI__ApiKey=<see Azure Portal – not shown here for security>"
echo "    AzureOpenAI__DeploymentName=$DEPLOYMENT_NAME"
echo "    AzureOpenAI__Model=$MODEL_NAME"
echo "    AzureOpenAI__UseDemoMode=false"
echo ""
echo "  Or add them to appsettings.json under the 'AzureOpenAI' section."
