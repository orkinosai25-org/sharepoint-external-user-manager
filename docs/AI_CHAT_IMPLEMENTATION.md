# AI Chat Assistant Implementation

## Overview

The ClientSpace portal now includes an AI-powered chat assistant that helps users learn about the product features, pricing, getting started, and SharePoint integration. The chat widget is integrated into the home page and provides intelligent responses using Azure OpenAI.

## Architecture

The implementation is based on the [Papagan conversational agent](https://github.com/orkinosai25-org/orkinosai-conversational-agent) architecture and consists of:

### Components

1. **DockableChatPanel.razor** - Interactive Blazor component with dockable UI
   - Location: `src/portal-blazor/SharePointExternalUserManager.Portal/Components/Chat/`
   - Features:
     - Dockable to 4 positions (Right, Bottom, Left, Top)
     - Welcome message with suggested topics
     - Message history with timestamps
     - Real-time typing indicator
     - Clear conversation functionality
     - Markdown-style formatting support

2. **ChatService.cs** - Service for handling AI interactions
   - Location: `src/portal-blazor/SharePointExternalUserManager.Portal/Services/`
   - Features:
     - Azure OpenAI integration
     - Demo mode fallback (works without Azure credentials)
     - Conversation history management
     - Intelligent context-aware responses

3. **ChatModels.cs** - Data models for chat functionality
   - Location: `src/portal-blazor/SharePointExternalUserManager.Portal/Models/`
   - Includes: ChatMessage, ChatRequest, ChatResponse, AzureOpenAISettings

### Styling

- **chat-widget.css** - Custom CSS for ClientSpace branding
  - Location: `src/portal-blazor/SharePointExternalUserManager.Portal/wwwroot/css/`
  - Features: Responsive design, animations, Microsoft-style UI

- **chat.js** - JavaScript utilities for scroll functionality
  - Location: `src/portal-blazor/SharePointExternalUserManager.Portal/wwwroot/js/`

## Configuration

### Azure OpenAI Setup (Optional)

The chat works out-of-the-box in **DEMO MODE** without any configuration. To enable full Azure OpenAI capabilities:

1. **Create Azure OpenAI Resource**
   - Go to [Azure Portal](https://portal.azure.com)
   - Create an Azure OpenAI resource
   - Deploy a GPT-4 model

2. **Configure Settings**

   Add to `appsettings.json` or use environment variables:

   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "https://your-resource.openai.azure.com/",
       "ApiKey": "",
       "DeploymentName": "gpt-4",
       "ApiVersion": "2024-08-01-preview",
       "Model": "gpt-4"
     }
   }
   ```

3. **Set API Key via User Secrets** (for development):

   ```bash
   dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR_API_KEY"
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
   ```

4. **Production Configuration**:
   - Use Azure Key Vault for storing API keys
   - Set via Azure App Service Configuration
   - Use managed identities when possible

### Demo Mode

When Azure OpenAI is not configured, the chat automatically runs in **DEMO MODE**:

- No Azure credentials required
- Provides intelligent mock responses
- Covers common questions about ClientSpace features
- Perfect for testing and demonstrations

Demo mode responses include:
- Product features and capabilities
- Pricing information guidance
- Getting started instructions
- SharePoint integration details

## Usage

### For End Users

1. **Open Chat**: Click the AI button in the bottom-right corner
2. **Ask Questions**: Type questions about ClientSpace in the input field
3. **Change Position**: Click the arrows icon to dock the chat to different screen edges
4. **Clear Chat**: Use the "Clear conversation" button to start fresh
5. **Close Chat**: Click the X button to minimize the chat widget

### For Developers

#### Integrating into Other Pages

```razor
@using SharePointExternalUserManager.Portal.Components.Chat

<DockableChatPanel @rendermode="InteractiveServer" 
                   IsOpen="isChatOpen" 
                   IsOpenChanged="HandleChatOpenChanged" />

@code {
    private bool isChatOpen = false;

    private void HandleChatOpenChanged(bool isOpen)
    {
        isChatOpen = isOpen;
    }
}
```

#### Customizing System Prompt

Edit `ChatService.cs` method `GetSystemPrompt()` to customize the AI's behavior and knowledge base.

#### Adding Training Data

The chat can be enhanced with additional training data by:

1. Expanding the demo responses in `GetDemoResponse()`
2. Configuring Azure OpenAI with custom fine-tuned models
3. Implementing RAG (Retrieval-Augmented Generation) with documentation

## Features

### Current Features

✅ **Interactive Chat Interface**
- Dockable panel with 4 position options
- Welcome message with suggested topics
- Real-time message exchange
- Typing indicator for AI responses
- Message timestamps

✅ **Demo Mode**
- Works without Azure OpenAI configuration
- Intelligent mock responses
- Perfect for testing and demonstrations

✅ **Azure OpenAI Integration**
- Full GPT-4 capabilities when configured
- Conversation context awareness
- Customizable system prompts
- Token usage tracking

✅ **ClientSpace Branding**
- Matches portal design system
- Microsoft-style UI elements
- Smooth animations and transitions
- Responsive mobile design

### Planned Features

🔄 **Document Training**
- Upload documentation for training
- Learn from product docs automatically
- URL-based knowledge ingestion

🔄 **Advanced Features**
- Multi-language support
- Voice input/output
- Conversation export
- Analytics dashboard

## Technical Details

### Dependencies

- **.NET 8** - Blazor Server
- **Azure OpenAI** - GPT-4 (optional)
- **Bootstrap Icons** - UI icons
- **SignalR** - Real-time communication (Blazor Server)

### Performance

- **Demo Mode**: Instant responses (< 100ms)
- **Azure OpenAI**: Typically 1-3 seconds depending on model and token count
- **Memory**: Conversation history stored in-memory per session
- **Scalability**: Consider Redis for distributed session state in production

### Security

- API keys never exposed to client
- All AI processing on server side
- Conversation history is session-scoped
- No persistent storage of conversations (privacy-first)

## Troubleshooting

### Chat Button Doesn't Respond

- Ensure `@rendermode="InteractiveServer"` is set on the component
- Check browser console for JavaScript errors
- Verify SignalR connection is established

### No AI Responses

- Check if running in Demo Mode (log message on startup)
- Verify Azure OpenAI configuration if using real AI
- Check API key and endpoint settings
- Review server logs for exceptions

### Styling Issues

- Verify `chat-widget.css` is referenced in `App.razor`
- Check browser console for 404 errors
- Clear browser cache

## Screenshots

See PR description for screenshots of:
- Chat button on home page
- Opened chat panel with welcome message
- Active conversation with AI responses
- Message formatting and timestamps

## References

- [Papagan Conversational Agent](https://github.com/orkinosai25-org/orkinosai-conversational-agent)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Blazor Server Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/)

## Support

For issues or questions:
1. Check this documentation
2. Review server logs
3. Consult the Papagan repository for architecture details
4. Contact the development team
