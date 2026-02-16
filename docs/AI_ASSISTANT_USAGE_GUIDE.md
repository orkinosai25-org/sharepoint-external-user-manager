# AI Assistant Usage Guide

## Overview

The ClientSpace AI Assistant provides intelligent help across two modes:
- **Marketing Mode**: Public Q&A for product information
- **In-Product Mode**: Context-aware guidance for authenticated users

## For End Users

### Accessing the AI Assistant

#### In the Blazor Portal
1. Look for the AI chat button in the bottom-right corner (blue circular button with chat icon)
2. Click to open the chat panel
3. Type your question and press Enter or click the send button
4. The assistant will respond with helpful information

#### Common Questions

**Marketing Mode (Public):**
- "What features does ClientSpace offer?"
- "How much does it cost?"
- "How do I get started?"
- "How does SharePoint integration work?"

**In-Product Mode (Authenticated):**
- "How do I add an external user to this space?"
- "Why can't a guest access the library?"
- "How do I create a new client space?"
- "What are the best practices for permissions?"

### AI Disclaimer

⚠️ **Important**: AI responses may be incorrect — verify before applying changes.

The AI assistant is a helpful tool, but:
- Always verify suggestions before implementing them
- Double-check permission changes
- Review generated content for accuracy
- Contact support for critical issues

### Features

- **Context-Aware**: In-product mode knows which client space you're in
- **Conversation History**: Maintains context within a session
- **Clear Conversation**: Start fresh at any time
- **Dockable**: Move the chat panel to any corner of the screen
- **Responsive**: Works on desktop, tablet, and mobile devices

## For Administrators

### Enabling/Disabling AI

1. Navigate to **Admin → AI Settings** in the Blazor portal
2. Toggle **Enable AI Assistant** on or off
3. Choose which modes to enable:
   - **Marketing Mode**: Public-facing AI
   - **In-Product Mode**: Authenticated user AI
4. Click **Save Changes**

### Configuring Usage Limits

#### Hourly Rate Limit
- Default: 100 requests per hour per tenant
- Range: 1-1000 requests
- Prevents abuse and controls costs

#### Token Budget
- **Max Tokens Per Request**: 100-4000 tokens (affects response length)
- **Monthly Token Budget**: Total tokens allowed per month (0 = unlimited)
- Tracks usage automatically and resets monthly

### Viewing Usage Statistics

The AI Settings page shows:
- **Tokens Used This Month**: Current consumption
- **Budget Percentage**: How much of monthly budget is used
- **Max Requests/Hour**: Current rate limit
- **Last Reset Date**: When the monthly counter reset

### Managing AI Disclaimer

Toggle **Show Disclaimer** to control whether users see the safety warning message. Recommended: Keep enabled for compliance and liability protection.

## For Developers

### Backend Configuration

#### Azure Key Vault Integration (Production)

Update `appsettings.json` to reference Key Vault:

```json
{
  "AzureOpenAI": {
    "Endpoint": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/AzureOpenAI-Endpoint/)",
    "ApiKey": "@Microsoft.KeyVault(SecretUri=https://your-keyvault.vault.azure.net/secrets/AzureOpenAI-ApiKey/)",
    "DeploymentName": "gpt-4",
    "ApiVersion": "2024-08-01-preview",
    "Model": "gpt-4"
  }
}
```

#### Local Development

Use user secrets for local development:

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api

dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key-here"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"
```

#### Demo Mode

If Azure OpenAI is not configured, the system automatically falls back to demo mode with intelligent pattern-matched responses. Perfect for:
- Development without Azure OpenAI subscription
- Testing UI and UX
- Demonstrations

### API Endpoints

#### Send Chat Message
```http
POST /api/aiassistant/chat
Content-Type: application/json
Authorization: Bearer {token} (for In-Product mode)

{
  "conversationId": "uuid",
  "message": "How do I add a user?",
  "mode": "InProduct",
  "context": {
    "clientSpaceId": 123,
    "clientSpaceName": "Acme Corp",
    "libraryName": "Shared Documents"
  },
  "temperature": 0.7,
  "maxTokens": 1000
}
```

#### Get AI Settings
```http
GET /api/aiassistant/settings
Authorization: Bearer {token}
```

#### Update AI Settings (Admin)
```http
PUT /api/aiassistant/settings
Authorization: Bearer {token}
Content-Type: application/json

{
  "isEnabled": true,
  "marketingModeEnabled": true,
  "inProductModeEnabled": true,
  "maxRequestsPerHour": 100,
  "maxTokensPerRequest": 1000,
  "monthlyTokenBudget": 500000,
  "showDisclaimer": true
}
```

#### Get Usage Statistics
```http
GET /api/aiassistant/usage
Authorization: Bearer {token}
```

### Database Schema

#### AiSettings Table
- `TenantId` (unique): Organization identifier
- `IsEnabled`: Master on/off switch
- `MarketingModeEnabled`: Public AI enabled
- `InProductModeEnabled`: Authenticated AI enabled
- `MaxRequestsPerHour`: Hourly rate limit
- `MaxTokensPerRequest`: Response size limit
- `MonthlyTokenBudget`: Monthly token allowance
- `TokensUsedThisMonth`: Current month usage
- `LastMonthlyReset`: Last budget reset date
- `ShowDisclaimer`: Display warning message

#### AiConversationLogs Table
- `TenantId` (nullable): Organization (null for marketing mode)
- `UserId`: User identifier
- `ConversationId`: Conversation grouping
- `Mode`: Marketing or InProduct
- `Context`: JSON context data
- `UserPrompt`: Sanitized user input
- `AssistantResponse`: AI response
- `TokensUsed`: Tokens consumed
- `ResponseTimeMs`: Response latency
- `Model`: AI model used
- `ErrorMessage`: Error details if failed

### Running Tests

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.Tests
dotnet test
```

Tests cover:
- Prompt sanitization and injection prevention
- Rate limiting logic and enforcement
- Tenant settings validation
- API response formats
- Demo mode functionality

### Security Considerations

1. **Never commit API keys**: Use Key Vault or user secrets
2. **Rate limiting**: Prevents abuse and controls costs
3. **Prompt sanitization**: Removes injection attempts
4. **Tenant isolation**: Complete data separation
5. **Audit logging**: All interactions logged
6. **HTTPS only**: Enforce secure connections

### Troubleshooting

#### AI Not Responding
- Check if AI is enabled in settings
- Verify Azure OpenAI configuration
- Check rate limit status
- Review application logs

#### Rate Limit Errors (HTTP 429)
- User exceeded hourly request limit
- Monthly token budget exhausted
- Admin can reset limits or increase quotas

#### Demo Mode Active
- Azure OpenAI not configured
- Check endpoint and API key settings
- Verify Key Vault references if using

## SPFx Integration (Future)

The backend API is ready for SharePoint Framework web parts:

```typescript
// SPFx integration example
const response = await this.context.httpClient.post(
  `${API_BASE}/api/aiassistant/chat`,
  HttpClient.configurations.v1,
  {
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${accessToken}`
    },
    body: JSON.stringify({
      conversationId: conversationId,
      message: userMessage,
      mode: 'InProduct',
      context: {
        clientSpaceId: this.properties.clientSpaceId,
        libraryName: this.context.pageContext.list.title
      }
    })
  }
);
```

## Support

For issues or questions:
1. Check this documentation
2. Review audit logs in the database
3. Contact support with conversation ID for assistance

---

**Last Updated**: February 11, 2026  
**Version**: 1.0
