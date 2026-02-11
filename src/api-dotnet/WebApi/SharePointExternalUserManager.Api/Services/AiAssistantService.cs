using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Azure OpenAI configuration
/// </summary>
public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4";
    public string ApiVersion { get; set; } = "2024-08-01-preview";
    public string Model { get; set; } = "gpt-4";
    public bool UseDemoMode { get; set; } = false;
}

/// <summary>
/// Service for AI assistant functionality with Azure OpenAI integration
/// </summary>
public class AiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIConfiguration _config;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        HttpClient httpClient,
        IOptions<AzureOpenAIConfiguration> config,
        ILogger<AiAssistantService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Generate AI response using Azure OpenAI
    /// </summary>
    public async Task<(string response, int tokensUsed)> GenerateResponseAsync(
        string userMessage,
        AiMode mode,
        AiContextInfo? context,
        List<(string role, string content)>? conversationHistory = null,
        double temperature = 0.7,
        int maxTokens = 1000,
        string? customSystemPrompt = null)
    {
        // Use demo mode if not configured
        if (_config.UseDemoMode || string.IsNullOrWhiteSpace(_config.Endpoint) || string.IsNullOrWhiteSpace(_config.ApiKey))
        {
            _logger.LogInformation("Using demo mode for AI response");
            return (GetDemoResponse(userMessage, mode, context), 100);
        }

        try
        {
            // Build system prompt based on mode
            var systemPrompt = customSystemPrompt ?? GetSystemPrompt(mode, context);

            // Build messages array
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add conversation history if provided
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var (role, messageContent) in conversationHistory)
                {
                    messages.Add(new { role, content = messageContent });
                }
            }

            // Add current user message
            messages.Add(new { role = "user", content = userMessage });

            // Build request
            var endpoint = $"{_config.Endpoint.TrimEnd('/')}/openai/deployments/{_config.DeploymentName}/chat/completions?api-version={_config.ApiVersion}";
            
            var requestBody = new
            {
                messages,
                temperature,
                max_tokens = maxTokens
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            requestMessage.Headers.Add("api-key", _config.ApiKey);

            // Make API call
            var response = await _httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Extract response and token usage
            var assistantMessage = jsonResponse
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "I apologize, but I couldn't generate a response.";

            var tokensUsed = 0;
            if (jsonResponse.TryGetProperty("usage", out var usage))
            {
                tokensUsed = usage.GetProperty("total_tokens").GetInt32();
            }

            return (assistantMessage, tokensUsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI API");
            throw;
        }
    }

    /// <summary>
    /// Sanitize user input to prevent prompt injection
    /// </summary>
    public string SanitizePrompt(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return string.Empty;

        // Remove potential prompt injection patterns
        var sanitized = userMessage
            .Replace("Ignore previous instructions", "")
            .Replace("ignore previous instructions", "")
            .Replace("System:", "")
            .Replace("system:", "")
            .Replace("Assistant:", "")
            .Replace("assistant:", "");

        // Limit length to prevent abuse
        const int maxLength = 2000;
        if (sanitized.Length > maxLength)
        {
            // Safely truncate without splitting multi-byte characters
            var textInfo = new System.Globalization.StringInfo(sanitized);
            if (textInfo.LengthInTextElements > maxLength)
            {
                sanitized = textInfo.SubstringByTextElements(0, maxLength);
            }
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Get system prompt based on mode and context
    /// </summary>
    private string GetSystemPrompt(AiMode mode, AiContextInfo? context)
    {
        if (mode == AiMode.Marketing)
        {
            return GetMarketingModePrompt();
        }
        else
        {
            return GetInProductModePrompt(context);
        }
    }

    /// <summary>
    /// Marketing mode system prompt
    /// </summary>
    private string GetMarketingModePrompt()
    {
        return @"You are a helpful AI assistant for ClientSpace, a SaaS platform for managing external users and client spaces in Microsoft 365 and SharePoint Online.

ClientSpace helps organizations:
- Manage external users with granular permissions
- Create dedicated SharePoint sites for each client (Client Spaces)
- Maintain security and compliance with multi-tenant architecture
- Track all activities with comprehensive audit logging
- Integrate billing through Stripe

Key Features:
1. External User Management: Invite, manage, and remove external users
2. Client Spaces: Dedicated SharePoint sites with isolated access
3. Library & List Management: Create and manage document libraries and lists
4. Security: Complete tenant isolation and audit trails
5. Blazor Portal: Admin dashboard for subscription and tenant management

Available Plans:
- Starter: Basic external user management, up to 5 client spaces
- Professional: Advanced features, up to 20 client spaces, AI setup guidance
- Business: Unlimited client spaces, AI audit explanations, priority support
- Enterprise: Custom solutions, AI governance insights, dedicated support

Architecture:
- SPFx web parts (installed in customer's SharePoint)
- ASP.NET Core backend API (multi-tenant)
- Blazor admin portal (SaaS platform)
- Azure infrastructure (SQL Database, Key Vault, App Insights)

Answer questions about the product, features, pricing, setup, and integration. Be friendly, concise, and helpful. Focus on business value and how ClientSpace solves collaboration challenges. If you don't know something specific, recommend checking the documentation or contacting support.";
    }

    /// <summary>
    /// In-product mode system prompt
    /// </summary>
    private string GetInProductModePrompt(AiContextInfo? context)
    {
        var basePrompt = @"You are an AI assistant integrated into ClientSpace, helping users perform tasks and understand the platform.

Your role:
- Help administrators manage external users and client spaces
- Explain SharePoint permissions and security concepts
- Guide users through setup and configuration
- Suggest best practices for external collaboration
- Answer ""how do I..."" questions with actionable steps
- Explain audit results and compliance features

Available actions (describe steps, don't execute):
- Add external users to a client space
- Create new client spaces
- Manage document library permissions
- Set up list templates
- Review audit logs
- Configure security settings

When giving instructions:
1. Provide clear, step-by-step guidance
2. Explain why each step matters
3. Mention relevant SharePoint concepts
4. Highlight security implications
5. Reference UI locations specifically

Be concise but thorough. Focus on practical, actionable advice.";

        if (context != null)
        {
            var contextInfo = new StringBuilder();
            contextInfo.AppendLine("\n\nCurrent Context:");

            if (!string.IsNullOrEmpty(context.ClientSpaceName))
                contextInfo.AppendLine($"- Client Space: {context.ClientSpaceName}");
            
            if (!string.IsNullOrEmpty(context.LibraryName))
                contextInfo.AppendLine($"- Library: {context.LibraryName}");
            
            if (!string.IsNullOrEmpty(context.CurrentPage))
                contextInfo.AppendLine($"- Current Page: {context.CurrentPage}");

            if (context.AdditionalData != null && context.AdditionalData.Any())
            {
                foreach (var kvp in context.AdditionalData)
                {
                    contextInfo.AppendLine($"- {kvp.Key}: {kvp.Value}");
                }
            }

            basePrompt += contextInfo.ToString();
        }

        return basePrompt;
    }

    /// <summary>
    /// Get demo response for when Azure OpenAI is not configured
    /// </summary>
    private string GetDemoResponse(string userMessage, AiMode mode, AiContextInfo? context)
    {
        var lowerMessage = userMessage.ToLower();

        if (mode == AiMode.Marketing)
        {
            return GetMarketingDemoResponse(lowerMessage);
        }
        else
        {
            return GetInProductDemoResponse(lowerMessage, context);
        }
    }

    private string GetMarketingDemoResponse(string lowerMessage)
    {
        if (lowerMessage.Contains("hello") || lowerMessage.Contains("hi"))
        {
            return "Hello! 👋 I'm the ClientSpace AI assistant. I can help you learn about our external collaboration platform for Microsoft 365. How can I assist you today?";
        }
        else if (lowerMessage.Contains("feature") || lowerMessage.Contains("what can"))
        {
            return "ClientSpace offers several key features:\n\n" +
                   "• **External User Management** - Easily invite and manage external users\n" +
                   "• **Client Spaces** - Create dedicated SharePoint sites for each client\n" +
                   "• **Security & Compliance** - Multi-tenant architecture with complete isolation\n" +
                   "• **Audit Logging** - Comprehensive activity tracking\n" +
                   "• **SPFx Integration** - Native SharePoint web parts\n\n" +
                   "Would you like to know more about any specific feature?";
        }
        else if (lowerMessage.Contains("price") || lowerMessage.Contains("cost") || lowerMessage.Contains("pricing") || lowerMessage.Contains("plan"))
        {
            return "ClientSpace offers flexible pricing plans:\n\n" +
                   "• **Starter** - $29/month - Up to 5 client spaces, basic features\n" +
                   "• **Professional** - $99/month - Up to 20 client spaces, AI guidance\n" +
                   "• **Business** - $299/month - Unlimited spaces, AI insights\n" +
                   "• **Enterprise** - Custom pricing - Advanced features, dedicated support\n\n" +
                   "All plans include a 14-day free trial. Would you like more details?";
        }
        else if (lowerMessage.Contains("how") || lowerMessage.Contains("setup") || lowerMessage.Contains("start") || lowerMessage.Contains("install"))
        {
            return "Getting started with ClientSpace is easy:\n\n" +
                   "1. **Sign up** - Create your account at our portal\n" +
                   "2. **Choose a plan** - Select the tier that fits your needs\n" +
                   "3. **Complete onboarding** - Follow the guided setup wizard\n" +
                   "4. **Install SPFx web parts** - Deploy to your SharePoint\n" +
                   "5. **Create your first client space** - Start collaborating!\n\n" +
                   "The entire process takes about 15 minutes. Need help with a specific step?";
        }
        else if (lowerMessage.Contains("sharepoint") || lowerMessage.Contains("microsoft 365") || lowerMessage.Contains("m365"))
        {
            return "ClientSpace integrates seamlessly with Microsoft 365 and SharePoint Online:\n\n" +
                   "• Uses Microsoft Graph API for native integration\n" +
                   "• SPFx web parts deploy directly to your SharePoint\n" +
                   "• Respects your existing security and permissions\n" +
                   "• Works with your Azure AD tenant\n" +
                   "• No data leaves your Microsoft 365 environment\n\n" +
                   "It's a 100% Microsoft-native solution!";
        }
        else if (lowerMessage.Contains("security") || lowerMessage.Contains("compliance") || lowerMessage.Contains("audit"))
        {
            return "Security and compliance are core to ClientSpace:\n\n" +
                   "• **Multi-tenant isolation** - Complete data separation\n" +
                   "• **Azure AD integration** - Secure authentication\n" +
                   "• **Audit logging** - Track all activities\n" +
                   "• **Role-based access** - Granular permissions\n" +
                   "• **SOC 2 compliance** - Enterprise-grade security\n\n" +
                   "Your data security is our top priority.";
        }
        else
        {
            return "Thank you for your question! I can help you learn about:\n\n" +
                   "• Features and capabilities\n" +
                   "• Pricing and plans\n" +
                   "• Getting started and setup\n" +
                   "• SharePoint integration\n" +
                   "• Security and compliance\n\n" +
                   "What would you like to know more about?";
        }
    }

    private string GetInProductDemoResponse(string lowerMessage, AiContextInfo? context)
    {
        var contextStr = context?.ClientSpaceName != null ? $" in {context.ClientSpaceName}" : "";

        if (lowerMessage.Contains("add") && lowerMessage.Contains("user"))
        {
            return $"To add an external user{contextStr}:\n\n" +
                   "1. Open the **External User Manager** web part\n" +
                   "2. Click **Add User** button\n" +
                   "3. Enter the user's email address\n" +
                   "4. Select appropriate permissions (Read, Edit, or Full Control)\n" +
                   "5. Click **Send Invitation**\n\n" +
                   "The user will receive an email invitation to access the space. Make sure they accept it within 90 days!";
        }
        else if (lowerMessage.Contains("permission") || lowerMessage.Contains("access"))
        {
            return "SharePoint permissions in ClientSpace work on three levels:\n\n" +
                   "• **Site level** - Controls access to the entire client space\n" +
                   "• **Library level** - Controls access to document libraries\n" +
                   "• **Item level** - Controls access to specific files\n\n" +
                   "Best practice: Grant permissions at the library level for easier management. Only use item-level for exceptional cases.\n\n" +
                   "Need help adjusting permissions for a specific user?";
        }
        else if (lowerMessage.Contains("create") && (lowerMessage.Contains("space") || lowerMessage.Contains("client")))
        {
            return "To create a new client space:\n\n" +
                   "1. Go to **Dashboard** in the Blazor portal\n" +
                   "2. Click **New Client Space** button\n" +
                   "3. Enter client name and reference code\n" +
                   "4. Choose a template (blank or with predefined libraries)\n" +
                   "5. Click **Create**\n\n" +
                   "The system will provision a new SharePoint site with isolated access. This usually takes 2-3 minutes.";
        }
        else if (lowerMessage.Contains("library") || lowerMessage.Contains("document"))
        {
            return $"Managing document libraries{contextStr}:\n\n" +
                   "• **Create**: Use the 'Add Library' button in the dashboard\n" +
                   "• **Configure**: Set versioning, approval workflows if needed\n" +
                   "• **Permissions**: Manage at library level for consistency\n" +
                   "• **Templates**: Use content types for document standards\n\n" +
                   "What specific task would you like help with?";
        }
        else if (lowerMessage.Contains("why") || lowerMessage.Contains("can't") || lowerMessage.Contains("cannot"))
        {
            return "If a user can't access something:\n\n" +
                   "1. **Check invitation status** - Did they accept it?\n" +
                   "2. **Verify permissions** - Do they have the right level?\n" +
                   "3. **Check expiry** - External access expires after 90 days\n" +
                   "4. **Review audit logs** - See recent access attempts\n\n" +
                   "Most issues are solved by re-sending the invitation or updating permissions.";
        }
        else if (lowerMessage.Contains("audit") || lowerMessage.Contains("log") || lowerMessage.Contains("activity"))
        {
            return "Audit logs track all activities:\n\n" +
                   "• User invitations and access\n" +
                   "• Permission changes\n" +
                   "• Document uploads and modifications\n" +
                   "• Space provisioning events\n\n" +
                   "View logs in the Dashboard → Audit section. Use filters to find specific events or users.";
        }
        else if (lowerMessage.Contains("best practice") || lowerMessage.Contains("recommend"))
        {
            return "Best practices for external collaboration:\n\n" +
                   "• Create separate client spaces for each client\n" +
                   "• Use library-level permissions, not item-level\n" +
                   "• Enable versioning on document libraries\n" +
                   "• Review audit logs monthly\n" +
                   "• Set expiration dates for temporary access\n" +
                   "• Document your permission strategy\n\n" +
                   "These practices help maintain security and manageability.";
        }
        else
        {
            return $"I'm here to help you{contextStr}! I can assist with:\n\n" +
                   "• Adding and managing external users\n" +
                   "• Understanding permissions and access\n" +
                   "• Creating and configuring client spaces\n" +
                   "• Managing libraries and documents\n" +
                   "• Reviewing audit logs\n" +
                   "• Following best practices\n\n" +
                   "What would you like help with?";
        }
    }
}
