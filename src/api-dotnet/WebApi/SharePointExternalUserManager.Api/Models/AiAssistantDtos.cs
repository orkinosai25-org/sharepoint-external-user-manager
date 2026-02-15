namespace SharePointExternalUserManager.Api.Models;

/// <summary>
/// AI assistant mode
/// </summary>
public enum AiMode
{
    /// <summary>
    /// Marketing mode - public, non-authenticated, product information
    /// </summary>
    Marketing,

    /// <summary>
    /// In-product mode - authenticated, context-aware, action guidance
    /// </summary>
    InProduct
}

/// <summary>
/// Request to send a message to AI assistant
/// </summary>
public class AiChatRequest
{
    /// <summary>
    /// Conversation ID for maintaining context
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// User message/prompt
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// AI mode (Marketing or InProduct)
    /// </summary>
    public AiMode Mode { get; set; } = AiMode.Marketing;

    /// <summary>
    /// Context information for InProduct mode (JSON)
    /// </summary>
    public AiContextInfo? Context { get; set; }

    /// <summary>
    /// Temperature for response generation (0.0 - 1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens for response
    /// </summary>
    public int MaxTokens { get; set; } = 1000;
}

/// <summary>
/// Context information for in-product AI mode
/// </summary>
public class AiContextInfo
{
    /// <summary>
    /// Current client space ID
    /// </summary>
    public int? ClientSpaceId { get; set; }

    /// <summary>
    /// Current client space name
    /// </summary>
    public string? ClientSpaceName { get; set; }

    /// <summary>
    /// Current library or list name
    /// </summary>
    public string? LibraryName { get; set; }

    /// <summary>
    /// Current page or section
    /// </summary>
    public string? CurrentPage { get; set; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, string>? AdditionalData { get; set; }
}

/// <summary>
/// Response from AI assistant
/// </summary>
public class AiChatResponse
{
    /// <summary>
    /// Conversation ID
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// User message that was sent
    /// </summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>
    /// AI assistant response
    /// </summary>
    public string AssistantMessage { get; set; } = string.Empty;

    /// <summary>
    /// Tokens used for this request
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Show disclaimer to user
    /// </summary>
    public bool ShowDisclaimer { get; set; } = true;

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// AI settings for a tenant
/// </summary>
public class AiSettingsDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public bool IsEnabled { get; set; }
    public bool MarketingModeEnabled { get; set; }
    public bool InProductModeEnabled { get; set; }
    public int MaxRequestsPerHour { get; set; }
    public int MaxTokensPerRequest { get; set; }
    public int MonthlyTokenBudget { get; set; }
    public int TokensUsedThisMonth { get; set; }
    public DateTime LastMonthlyReset { get; set; }
    public bool ShowDisclaimer { get; set; }
    public string? CustomSystemPrompt { get; set; }
}

/// <summary>
/// Request to update AI settings
/// </summary>
public class UpdateAiSettingsRequest
{
    public bool? IsEnabled { get; set; }
    public bool? MarketingModeEnabled { get; set; }
    public bool? InProductModeEnabled { get; set; }
    public int? MaxRequestsPerHour { get; set; }
    public int? MaxTokensPerRequest { get; set; }
    public int? MonthlyTokenBudget { get; set; }
    public bool? ShowDisclaimer { get; set; }
    public string? CustomSystemPrompt { get; set; }
}

/// <summary>
/// AI usage statistics
/// </summary>
public class AiUsageStats
{
    public int TenantId { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public int TokensUsedThisMonth { get; set; }
    public int MonthlyTokenBudget { get; set; }
    public decimal BudgetUsedPercentage { get; set; }
    public int RequestsThisHour { get; set; }
    public int MaxRequestsPerHour { get; set; }
    public int AverageResponseTimeMs { get; set; }
    public Dictionary<string, int> MessagesByMode { get; set; } = new();
}
