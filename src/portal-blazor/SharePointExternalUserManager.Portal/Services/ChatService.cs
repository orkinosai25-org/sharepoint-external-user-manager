using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ChatService> _logger;
    
    // NOTE: In-memory storage is used for simplicity in this implementation.
    // For production use with multiple server instances, consider using:
    // - IDistributedCache (Redis) for conversation persistence
    // - Database storage for conversation history
    // - Session state with a backing store
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();

    public ChatService(IOptions<ApiSettings> apiSettings, HttpClient httpClient, ILogger<ChatService> logger)
    {
        _apiSettings = apiSettings.Value;
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure HttpClient base address if API is configured
        if (!string.IsNullOrWhiteSpace(_apiSettings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_apiSettings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_apiSettings.Timeout);
        }
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        // Create conversation if it doesn't exist
        if (!_conversations.ContainsKey(request.ConversationId))
        {
            _conversations[request.ConversationId] = new List<ChatMessage>();
        }

        // Add user message to conversation history
        _conversations[request.ConversationId].Add(new ChatMessage
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });

        ChatResponse response;

        try
        {
            // Call backend API
            response = await CallBackendApiAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API");
            
            // Return error message
            response = new ChatResponse
            {
                ConversationId = request.ConversationId,
                UserMessage = request.Message,
                AssistantMessage = "I'm sorry, I encountered an error processing your request. Please try again later.",
                ShowDisclaimer = true,
                Timestamp = DateTime.UtcNow
            };
        }

        // Add assistant message to conversation history
        _conversations[request.ConversationId].Add(new ChatMessage
        {
            Role = "assistant",
            Content = response.AssistantMessage,
            Timestamp = DateTime.UtcNow
        });

        return response;
    }

    private async Task<ChatResponse> CallBackendApiAsync(ChatRequest request)
    {
        // Check if API is configured
        if (string.IsNullOrWhiteSpace(_apiSettings.BaseUrl) || 
            _apiSettings.BaseUrl.Contains("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("API URL not configured, using demo mode");
            return GetDemoResponse(request);
        }

        try
        {
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync("/aiassistant/chat", content);
            
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("API returned error {StatusCode}: {Error}", 
                    httpResponse.StatusCode, errorContent);
                throw new HttpRequestException($"API returned {httpResponse.StatusCode}");
            }

            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<ChatResponse>(responseContent, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return response ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API");
            throw;
        }
    }

    private ChatResponse GetDemoResponse(ChatRequest request)
    {
        var lowerMessage = request.Message.ToLower();
        string assistantMessage;

        if (request.Mode == "InProduct")
        {
            assistantMessage = GetInProductDemoResponse(lowerMessage, request.Context);
        }
        else
        {
            assistantMessage = GetMarketingDemoResponse(lowerMessage);
        }

        return new ChatResponse
        {
            ConversationId = request.ConversationId,
            UserMessage = request.Message,
            AssistantMessage = assistantMessage,
            ShowDisclaimer = true,
            Timestamp = DateTime.UtcNow
        };
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
                   "• **Audit Logging** - Comprehensive activity tracking\n\n" +
                   "Would you like to know more about any specific feature?";
        }
        else if (lowerMessage.Contains("price") || lowerMessage.Contains("cost") || lowerMessage.Contains("pricing"))
        {
            return "ClientSpace offers flexible pricing plans to suit different needs. Visit our Pricing page to see the available tiers and features. Would you like me to explain more about our subscription options?";
        }
        else if (lowerMessage.Contains("how") || lowerMessage.Contains("setup") || lowerMessage.Contains("start"))
        {
            return "Getting started with ClientSpace is easy:\n\n" +
                   "1. Sign up for an account\n" +
                   "2. Choose your pricing plan\n" +
                   "3. Complete the onboarding wizard\n" +
                   "4. Install the SPFx web parts in your SharePoint\n" +
                   "5. Start managing external users!\n\n" +
                   "Would you like more details on any of these steps?";
        }
        else if (lowerMessage.Contains("sharepoint") || lowerMessage.Contains("microsoft 365"))
        {
            return "ClientSpace integrates seamlessly with Microsoft 365 and SharePoint Online. It uses the Microsoft Graph API to manage sites, users, and permissions while maintaining security and compliance standards. Our solution is fully Microsoft-native!";
        }
        else
        {
            return $"Thank you for your question! I can help you learn about:\n\n" +
                   "• Features and capabilities\n" +
                   "• Pricing and plans\n" +
                   "• Getting started\n" +
                   "• SharePoint integration\n\n" +
                   "What would you like to know more about?";
        }
    }

    private string GetInProductDemoResponse(string lowerMessage, ChatContextInfo? context)
    {
        var contextStr = context?.ClientSpaceName != null ? $" in {context.ClientSpaceName}" : "";

        if (lowerMessage.Contains("add") && lowerMessage.Contains("user"))
        {
            return $"To add an external user{contextStr}:\n\n" +
                   "1. Navigate to the **External User Manager**\n" +
                   "2. Click **Add User**\n" +
                   "3. Enter their email address\n" +
                   "4. Select permissions\n" +
                   "5. Click **Send Invitation**\n\n" +
                   "They'll receive an email to access the space.";
        }
        else if (lowerMessage.Contains("permission"))
        {
            return "SharePoint permissions work on three levels:\n\n" +
                   "• **Site level** - Full site access\n" +
                   "• **Library level** - Document library access\n" +
                   "• **Item level** - Specific file access\n\n" +
                   "Best practice: Use library-level permissions for easier management.";
        }
        else
        {
            return $"I'm here to help you{contextStr}! I can assist with:\n\n" +
                   "• Adding external users\n" +
                   "• Managing permissions\n" +
                   "• Creating client spaces\n" +
                   "• Understanding features\n\n" +
                   "What would you like help with?";
        }
    }

    public List<ChatMessage> GetConversationHistory(string conversationId)
    {
        return _conversations.ContainsKey(conversationId)
            ? _conversations[conversationId]
            : new List<ChatMessage>();
    }

    public void ClearConversation(string conversationId)
    {
        if (_conversations.ContainsKey(conversationId))
        {
            _conversations[conversationId].Clear();
        }
    }
}
