using System.Text;
using SharePointExternalUserManager.Api.Models;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service for managing AI prompt templates with context awareness
/// </summary>
public class PromptTemplateService
{
    private readonly ILogger<PromptTemplateService> _logger;

    public PromptTemplateService(ILogger<PromptTemplateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate a context-aware system prompt
    /// </summary>
    public string GenerateSystemPrompt(
        AiMode mode,
        AiContextInfo? context,
        string? customSystemPrompt = null,
        EnhancedContextInfo? enhancedContext = null)
    {
        // Use custom prompt if provided
        if (!string.IsNullOrWhiteSpace(customSystemPrompt))
        {
            return customSystemPrompt;
        }

        // Generate prompt based on mode
        if (mode == AiMode.Marketing)
        {
            return GetMarketingModePrompt();
        }
        else
        {
            return GetInProductModePrompt(context, enhancedContext);
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
    /// In-product mode system prompt with enhanced context
    /// </summary>
    private string GetInProductModePrompt(AiContextInfo? context, EnhancedContextInfo? enhancedContext)
    {
        var prompt = new StringBuilder();

        // Base prompt with role-specific guidance
        prompt.AppendLine(@"You are an AI assistant integrated into ClientSpace, helping users perform tasks and understand the platform.

Your role:
- Help administrators manage external users and client spaces
- Explain SharePoint permissions and security concepts
- Guide users through setup and configuration
- Suggest best practices for external collaboration
- Answer ""how do I..."" questions with actionable steps
- Explain audit results and compliance features");

        // Add role-specific guidance
        if (enhancedContext?.UserRole != null)
        {
            prompt.AppendLine();
            prompt.AppendLine(GetRoleSpecificGuidance(enhancedContext.UserRole));
        }

        // Add subscription tier context
        if (enhancedContext?.SubscriptionTier != null)
        {
            prompt.AppendLine();
            prompt.AppendLine(GetTierSpecificGuidance(enhancedContext.SubscriptionTier));
        }

        prompt.AppendLine(@"
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

Be concise but thorough. Focus on practical, actionable advice.");

        // Add current context
        if (context != null || enhancedContext != null)
        {
            prompt.AppendLine();
            prompt.AppendLine(BuildContextSection(context, enhancedContext));
        }

        return prompt.ToString();
    }

    /// <summary>
    /// Get role-specific guidance text
    /// </summary>
    private string GetRoleSpecificGuidance(string userRole)
    {
        return userRole.ToLower() switch
        {
            "admin" => @"User Role: Administrator
- You have full system access including tenant settings, billing, and all client spaces
- Focus on governance, security best practices, and system configuration
- Provide guidance on tenant-wide policies and subscription management",

            "user" => @"User Role: Standard User
- You have access to assigned client spaces only
- Focus on day-to-day collaboration tasks and user management
- Avoid discussing admin-only features or tenant-wide settings",

            _ => $"User Role: {userRole}\n- Provide guidance appropriate to this user's permission level"
        };
    }

    /// <summary>
    /// Get subscription tier-specific guidance
    /// </summary>
    private string GetTierSpecificGuidance(string subscriptionTier)
    {
        return subscriptionTier switch
        {
            "Starter" => @"Subscription Tier: Starter
- Up to 5 client spaces
- Basic features available
- Inform about Professional tier features if relevant (e.g., AI guidance, advanced audit)",

            "Professional" => @"Subscription Tier: Professional
- Up to 20 client spaces
- AI setup guidance enabled
- Advanced audit and reporting available
- Suggest Business tier for unlimited client spaces if needed",

            "Business" => @"Subscription Tier: Business
- Unlimited client spaces
- AI audit explanations enabled
- Priority support available
- Full feature access except enterprise customizations",

            "Enterprise" => @"Subscription Tier: Enterprise
- Full feature access with custom solutions
- AI governance insights enabled
- Dedicated support
- Custom integrations available",

            _ => $"Subscription Tier: {subscriptionTier}"
        };
    }

    /// <summary>
    /// Build current context section
    /// </summary>
    private string BuildContextSection(AiContextInfo? context, EnhancedContextInfo? enhancedContext)
    {
        var contextInfo = new StringBuilder();
        contextInfo.AppendLine("Current Context:");

        // Basic context
        if (context != null)
        {
            if (!string.IsNullOrEmpty(context.ClientSpaceName))
                contextInfo.AppendLine($"- Client Space: {context.ClientSpaceName}");

            if (!string.IsNullOrEmpty(context.LibraryName))
                contextInfo.AppendLine($"- Library: {context.LibraryName}");

            if (!string.IsNullOrEmpty(context.CurrentPage))
            {
                contextInfo.AppendLine($"- Current Page: {context.CurrentPage}");
                contextInfo.AppendLine(GetPageSpecificGuidance(context.CurrentPage));
            }

            if (context.AdditionalData != null && context.AdditionalData.Any())
            {
                foreach (var kvp in context.AdditionalData)
                {
                    contextInfo.AppendLine($"- {kvp.Key}: {kvp.Value}");
                }
            }
        }

        // Enhanced context
        if (enhancedContext != null)
        {
            if (!string.IsNullOrEmpty(enhancedContext.CurrentScreen))
                contextInfo.AppendLine($"- Screen: {enhancedContext.CurrentScreen}");

            if (enhancedContext.UserPermissionLevel != null)
                contextInfo.AppendLine($"- Permission Level: {enhancedContext.UserPermissionLevel}");

            if (enhancedContext.TenantId > 0)
                contextInfo.AppendLine($"- Tenant ID: {enhancedContext.TenantId}");
        }

        return contextInfo.ToString();
    }

    /// <summary>
    /// Get page-specific guidance
    /// </summary>
    private string GetPageSpecificGuidance(string currentPage)
    {
        var guidance = currentPage.ToLower() switch
        {
            var p when p.Contains("dashboard") => 
                "  Focus on: Overview metrics, recent activities, quick actions for client space management",

            var p when p.Contains("client") && p.Contains("space") => 
                "  Focus on: Creating/managing client spaces, site permissions, library setup",

            var p when p.Contains("external") && p.Contains("user") => 
                "  Focus on: Inviting users, managing permissions, access expiry, troubleshooting access issues",

            var p when p.Contains("library") || p.Contains("document") => 
                "  Focus on: Document management, versioning, permissions, content types",

            var p when p.Contains("audit") || p.Contains("log") => 
                "  Focus on: Activity tracking, compliance reports, security reviews",

            var p when p.Contains("settings") || p.Contains("config") => 
                "  Focus on: Tenant configuration, subscription management, security settings",

            var p when p.Contains("billing") || p.Contains("subscription") => 
                "  Focus on: Plan selection, billing details, feature limits, upgrades",

            _ => ""
        };

        return guidance;
    }
}

/// <summary>
/// Enhanced context information for prompt generation
/// </summary>
public class EnhancedContextInfo
{
    public int TenantId { get; set; }
    public string? UserRole { get; set; }
    public string? SubscriptionTier { get; set; }
    public string? UserPermissionLevel { get; set; }
    public string? CurrentScreen { get; set; }
}
