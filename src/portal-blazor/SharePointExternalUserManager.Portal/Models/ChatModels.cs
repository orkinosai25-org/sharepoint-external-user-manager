namespace SharePointExternalUserManager.Portal.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChatRequest
{
    public string ConversationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Mode { get; set; } = "Marketing"; // "Marketing" or "InProduct"
    public ChatContextInfo? Context { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
}

public class ChatContextInfo
{
    public int? ClientSpaceId { get; set; }
    public string? ClientSpaceName { get; set; }
    public string? LibraryName { get; set; }
    public string? CurrentPage { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
}

public class ChatResponse
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public string AssistantMessage { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public bool ShowDisclaimer { get; set; } = true;
    public DateTime Timestamp { get; set; }
}

public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-08-01-preview";
    public string Model { get; set; } = "gpt-4";
}
