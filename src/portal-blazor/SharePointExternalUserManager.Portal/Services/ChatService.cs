using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SharePointExternalUserManager.Portal.Models;

namespace SharePointExternalUserManager.Portal.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<ChatService> _logger;
    private readonly Dictionary<string, List<ChatMessage>> _conversations = new();
    private readonly bool _isDemoMode;

    public ChatService(IOptions<AzureOpenAISettings> settings, HttpClient httpClient, ILogger<ChatService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
        
        // Check if we're in demo mode (no valid Azure OpenAI configuration)
        _isDemoMode = string.IsNullOrWhiteSpace(_settings.Endpoint) ||
                      string.IsNullOrWhiteSpace(_settings.ApiKey) ||
                      _settings.Endpoint.Contains("YOUR_", StringComparison.OrdinalIgnoreCase) ||
                      _settings.ApiKey.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

        if (_isDemoMode)
        {
            _logger.LogWarning("ChatService running in DEMO MODE - using mock responses. Configure Azure OpenAI to enable full functionality.");
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

        string assistantMessage;

        if (_isDemoMode)
        {
            // Demo mode - return mock response
            assistantMessage = GetDemoResponse(request.Message);
        }
        else
        {
            // Real mode - call Azure OpenAI
            try
            {
                assistantMessage = await CallAzureOpenAIAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI API");
                assistantMessage = "I'm sorry, I encountered an error processing your request. Please try again later.";
            }
        }

        // Add assistant message to conversation history
        _conversations[request.ConversationId].Add(new ChatMessage
        {
            Role = "assistant",
            Content = assistantMessage,
            Timestamp = DateTime.UtcNow
        });

        return new ChatResponse
        {
            ConversationId = request.ConversationId,
            UserMessage = request.Message,
            AssistantMessage = assistantMessage,
            Timestamp = DateTime.UtcNow
        };
    }

    private string GetDemoResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();
        
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
            return $"Thank you for your question! I'm currently running in demo mode. To enable full AI capabilities with detailed answers about ClientSpace, an Azure OpenAI configuration is needed. In the meantime, try asking about features, pricing, or getting started!";
        }
    }

    private async Task<string> CallAzureOpenAIAsync(ChatRequest request)
    {
        // Build the API endpoint
        var endpoint = $"{_settings.Endpoint.TrimEnd('/')}/openai/deployments/{_settings.DeploymentName}/chat/completions?api-version={_settings.ApiVersion}";

        // Build messages array from conversation history
        var messages = new List<object>
        {
            new
            {
                role = "system",
                content = GetSystemPrompt()
            }
        };

        // Add conversation history
        if (_conversations.ContainsKey(request.ConversationId))
        {
            foreach (var msg in _conversations[request.ConversationId])
            {
                messages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }
        }

        // Build request body
        var requestBody = new
        {
            messages = messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Add API key header
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);

        // Make the API call
        var response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Extract the assistant's message
        var assistantMessage = jsonResponse
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "I apologize, but I couldn't generate a response.";

        return assistantMessage;
    }

    private string GetSystemPrompt()
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

Architecture:
- SPFx web parts (installed in customer's SharePoint)
- ASP.NET Core backend API (multi-tenant)
- Blazor admin portal (SaaS platform)
- Azure infrastructure (SQL Database, Key Vault, App Insights)

Answer questions about the product, features, pricing, setup, and integration. Be friendly, concise, and helpful. If you don't know something specific, recommend checking the documentation or contacting support.";
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
