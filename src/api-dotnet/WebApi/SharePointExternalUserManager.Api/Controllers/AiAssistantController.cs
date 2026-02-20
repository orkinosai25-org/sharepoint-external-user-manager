using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;
using SharePointExternalUserManager.Api.Attributes;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiAssistantController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AiAssistantService _aiService;
    private readonly AiRateLimitService _rateLimitService;
    private readonly PromptTemplateService _promptTemplateService;
    private readonly ILogger<AiAssistantController> _logger;

    public AiAssistantController(
        ApplicationDbContext context,
        AiAssistantService aiService,
        AiRateLimitService rateLimitService,
        PromptTemplateService promptTemplateService,
        ILogger<AiAssistantController> logger)
    {
        _context = context;
        _aiService = aiService;
        _rateLimitService = rateLimitService;
        _promptTemplateService = promptTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the AI assistant
    /// </summary>
    [HttpPost("chat")]
    [RequiresPlan("Pro", "AI Assistant")]
    public async Task<ActionResult<AiChatResponse>> SendMessage([FromBody] AiChatRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        int? tenantId = null;
        AiSettingsEntity? settings = null;
        string? userRole = null;
        string? userId = null;

        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            // Get tenant ID and user info from claims if authenticated (for InProduct mode)
            if (User.Identity?.IsAuthenticated == true && request.Mode == AiMode.InProduct)
            {
                // Check if user is a guest - deny access to guests
                var userPrincipalName = User.FindFirst("preferred_username")?.Value 
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                
                if (!string.IsNullOrEmpty(userPrincipalName) && IsGuestUser(userPrincipalName))
                {
                    _logger.LogWarning("Guest user {UserPrincipalName} attempted to access AI assistant", userPrincipalName);
                    return StatusCode(403, "AI assistant is not available for guest users");
                }

                var tenantIdClaim = User.FindFirst("TenantId")?.Value;
                if (int.TryParse(tenantIdClaim, out var tid))
                {
                    tenantId = tid;
                }

                // Get user role from claims
                userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                    ?? (User.IsInRole("Admin") ? "Admin" : "User");
                
                userId = User.Identity?.Name;
            }

            // Get AI settings for tenant (or use defaults for marketing mode)
            if (tenantId.HasValue)
            {
                settings = await _context.AiSettings
                    .FirstOrDefaultAsync(s => s.TenantId == tenantId.Value);

                if (settings == null)
                {
                    // Create default settings for tenant
                    settings = new AiSettingsEntity
                    {
                        TenantId = tenantId.Value,
                        IsEnabled = true,
                        MarketingModeEnabled = true,
                        InProductModeEnabled = true
                    };
                    _context.AiSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                // Check if AI is enabled for this tenant
                if (!settings.IsEnabled)
                {
                    return StatusCode(403, "AI assistant is disabled for your organization");
                }

                // Check if requested mode is enabled
                if (request.Mode == AiMode.Marketing && !settings.MarketingModeEnabled)
                {
                    return StatusCode(403, "Marketing AI mode is disabled for your organization");
                }
                if (request.Mode == AiMode.InProduct && !settings.InProductModeEnabled)
                {
                    return StatusCode(403, "In-product AI mode is disabled for your organization");
                }

                // Check rate limit
                if (_rateLimitService.IsRateLimitExceeded(tenantId, settings.MaxRequestsPerHour))
                {
                    return StatusCode(429, "Rate limit exceeded. Please try again later.");
                }

                // Check monthly token budget
                if (settings.MonthlyTokenBudget > 0 && settings.TokensUsedThisMonth >= settings.MonthlyTokenBudget)
                {
                    return StatusCode(429, "Monthly token budget exceeded");
                }

                // Check plan-based monthly message limit
                var subscription = await _context.Subscriptions
                    .Where(s => s.TenantId == tenantId.Value)
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    // Get plan definition for the subscription tier
                    var planDef = PlanConfiguration.GetPlanDefinitionByName(subscription.Tier);
                    if (planDef != null && planDef.Limits.MaxAiMessagesPerMonth.HasValue)
                    {
                        // Count messages this month for this tenant
                        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        var messagesThisMonth = await _context.AiConversationLogs
                            .Where(l => l.TenantId == tenantId.Value && l.Timestamp >= startOfMonth)
                            .CountAsync();

                        if (messagesThisMonth >= planDef.Limits.MaxAiMessagesPerMonth.Value)
                        {
                            return StatusCode(429, new
                            {
                                error = "MessageLimitExceeded",
                                message = $"Monthly AI message limit of {planDef.Limits.MaxAiMessagesPerMonth.Value} messages exceeded for {planDef.Name} plan. Upgrade to send more messages.",
                                currentUsage = messagesThisMonth,
                                limit = planDef.Limits.MaxAiMessagesPerMonth.Value,
                                planTier = planDef.Name
                            });
                        }
                    }
                }

                // Reset monthly counter if needed
                if (settings.LastMonthlyReset.Month != DateTime.UtcNow.Month ||
                    settings.LastMonthlyReset.Year != DateTime.UtcNow.Year)
                {
                    settings.TokensUsedThisMonth = 0;
                    settings.LastMonthlyReset = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // Sanitize user input
            var sanitizedMessage = _aiService.SanitizePrompt(request.Message);

            // Get conversation history (last 10 messages)
            var conversationHistory = await GetConversationHistory(request.ConversationId, 10);

            // Build enhanced context
            var enhancedContext = await BuildEnhancedContext(tenantId, userRole);

            // Generate context-aware system prompt
            var systemPrompt = _promptTemplateService.GenerateSystemPrompt(
                request.Mode,
                request.Context,
                settings?.CustomSystemPrompt,
                enhancedContext
            );

            // Generate AI response
            var (assistantMessage, tokensUsed) = await _aiService.GenerateResponseAsync(
                sanitizedMessage,
                systemPrompt,
                conversationHistory,
                request.Temperature,
                Math.Min(request.MaxTokens, settings?.MaxTokensPerRequest ?? 1000)
            );

            stopwatch.Stop();

            // Update token usage
            if (tenantId.HasValue && settings != null)
            {
                settings.TokensUsedThisMonth += tokensUsed;
                _rateLimitService.IncrementRequestCount(tenantId);
                await _context.SaveChangesAsync();
            }

            // Log conversation with enhanced context
            var logEntry = new AiConversationLogEntity
            {
                TenantId = tenantId,
                UserId = userId,
                ConversationId = request.ConversationId,
                Mode = request.Mode.ToString(),
                Context = BuildContextJson(request.Context, enhancedContext),
                UserPrompt = sanitizedMessage,
                AssistantResponse = assistantMessage,
                TokensUsed = tokensUsed,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                Model = "gpt-4"
            };
            _context.AiConversationLogs.Add(logEntry);
            await _context.SaveChangesAsync();

            // Return response
            return Ok(new AiChatResponse
            {
                ConversationId = request.ConversationId,
                UserMessage = request.Message,
                AssistantMessage = assistantMessage,
                TokensUsed = tokensUsed,
                ShowDisclaimer = settings?.ShowDisclaimer ?? true,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI chat request");

            // Enhanced error handling with fallback messages
            string errorMessage;
            if (ex is HttpRequestException)
            {
                errorMessage = "Unable to connect to the AI service. Please check your connection and try again.";
            }
            else if (ex is TaskCanceledException || ex is TimeoutException)
            {
                errorMessage = "The request timed out. Please try again with a shorter message.";
            }
            else
            {
                errorMessage = "An error occurred while processing your request. Our team has been notified.";
            }

            // Log error with enhanced context
            var errorLog = new AiConversationLogEntity
            {
                TenantId = tenantId,
                UserId = userId,
                ConversationId = request.ConversationId,
                Mode = request.Mode.ToString(),
                Context = request.Context != null ? JsonSerializer.Serialize(request.Context) : null,
                UserPrompt = request.Message,
                AssistantResponse = "Error",
                TokensUsed = 0,
                ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorMessage = ex.Message
            };
            _context.AiConversationLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            return StatusCode(500, errorMessage);
        }
    }

    /// <summary>
    /// Get AI settings for current tenant
    /// </summary>
    [HttpGet("settings")]
    [Authorize]
    public async Task<ActionResult<AiSettingsDto>> GetSettings()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Tenant ID not found in claims");
        }

        var settings = await _context.AiSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings == null)
        {
            // Create default settings
            settings = new AiSettingsEntity
            {
                TenantId = tenantId,
                IsEnabled = true,
                MarketingModeEnabled = true,
                InProductModeEnabled = true
            };
            _context.AiSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(MapToDto(settings));
    }

    /// <summary>
    /// Update AI settings for current tenant (admin only)
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AiSettingsDto>> UpdateSettings([FromBody] UpdateAiSettingsRequest request)
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Tenant ID not found in claims");
        }

        var settings = await _context.AiSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        if (settings == null)
        {
            settings = new AiSettingsEntity { TenantId = tenantId };
            _context.AiSettings.Add(settings);
        }

        // Update only provided fields
        if (request.IsEnabled.HasValue)
            settings.IsEnabled = request.IsEnabled.Value;
        
        if (request.MarketingModeEnabled.HasValue)
            settings.MarketingModeEnabled = request.MarketingModeEnabled.Value;
        
        if (request.InProductModeEnabled.HasValue)
            settings.InProductModeEnabled = request.InProductModeEnabled.Value;
        
        if (request.MaxRequestsPerHour.HasValue)
            settings.MaxRequestsPerHour = Math.Max(1, request.MaxRequestsPerHour.Value);
        
        if (request.MaxTokensPerRequest.HasValue)
            settings.MaxTokensPerRequest = Math.Max(100, Math.Min(4000, request.MaxTokensPerRequest.Value));
        
        if (request.MonthlyTokenBudget.HasValue)
            settings.MonthlyTokenBudget = Math.Max(0, request.MonthlyTokenBudget.Value);
        
        if (request.ShowDisclaimer.HasValue)
            settings.ShowDisclaimer = request.ShowDisclaimer.Value;
        
        if (request.CustomSystemPrompt != null)
            settings.CustomSystemPrompt = string.IsNullOrWhiteSpace(request.CustomSystemPrompt) 
                ? null 
                : request.CustomSystemPrompt;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(settings));
    }

    /// <summary>
    /// Get AI usage statistics for current tenant
    /// </summary>
    [HttpGet("usage")]
    [Authorize]
    public async Task<ActionResult<AiUsageStats>> GetUsageStats()
    {
        var tenantIdClaim = User.FindFirst("TenantId")?.Value;
        if (!int.TryParse(tenantIdClaim, out var tenantId))
        {
            return BadRequest("Tenant ID not found in claims");
        }

        var settings = await _context.AiSettings
            .FirstOrDefaultAsync(s => s.TenantId == tenantId);

        var logs = await _context.AiConversationLogs
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();

        var thisMonth = logs.Where(l => 
            l.Timestamp.Month == DateTime.UtcNow.Month && 
            l.Timestamp.Year == DateTime.UtcNow.Year);

        // Get subscription and plan limits for message count
        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        int? maxMessagesPerMonth = null;
        decimal? messageLimitUsedPercentage = null;
        string? planTier = null;
        int messagesThisMonthCount = thisMonth.Count();

        if (subscription != null)
        {
            planTier = subscription.Tier;
            var planDef = PlanConfiguration.GetPlanDefinitionByName(subscription.Tier);
            if (planDef != null)
            {
                maxMessagesPerMonth = planDef.Limits.MaxAiMessagesPerMonth;
                if (maxMessagesPerMonth.HasValue && maxMessagesPerMonth.Value > 0)
                {
                    messageLimitUsedPercentage = (decimal)messagesThisMonthCount / maxMessagesPerMonth.Value * 100;
                }
            }
        }

        var stats = new AiUsageStats
        {
            TenantId = tenantId,
            TotalConversations = logs.Select(l => l.ConversationId).Distinct().Count(),
            TotalMessages = logs.Count,
            MessagesThisMonth = messagesThisMonthCount,
            MaxMessagesPerMonth = maxMessagesPerMonth,
            MessageLimitUsedPercentage = messageLimitUsedPercentage,
            TokensUsedThisMonth = settings?.TokensUsedThisMonth ?? 0,
            MonthlyTokenBudget = settings?.MonthlyTokenBudget ?? 0,
            BudgetUsedPercentage = settings != null && settings.MonthlyTokenBudget > 0
                ? (decimal)settings.TokensUsedThisMonth / settings.MonthlyTokenBudget * 100
                : 0,
            RequestsThisHour = logs.Count(l => l.Timestamp >= DateTime.UtcNow.AddHours(-1)),
            MaxRequestsPerHour = settings?.MaxRequestsPerHour ?? 100,
            AverageResponseTimeMs = logs.Any() ? (int)logs.Average(l => l.ResponseTimeMs) : 0,
            MessagesByMode = logs.GroupBy(l => l.Mode)
                .ToDictionary(g => g.Key, g => g.Count()),
            PlanTier = planTier
        };

        return Ok(stats);
    }

    /// <summary>
    /// Clear conversation history
    /// </summary>
    [HttpDelete("conversations/{conversationId}")]
    public async Task<IActionResult> ClearConversation(string conversationId)
    {
        var logs = await _context.AiConversationLogs
            .Where(l => l.ConversationId == conversationId)
            .ToListAsync();

        _context.AiConversationLogs.RemoveRange(logs);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Helper methods

    /// <summary>
    /// Check if user is a guest user
    /// </summary>
    private bool IsGuestUser(string userPrincipalName)
    {
        if (string.IsNullOrEmpty(userPrincipalName))
            return false;

        // Guest users typically have #EXT# in their UPN or specific patterns
        return userPrincipalName.Contains("#EXT#", StringComparison.OrdinalIgnoreCase) ||
               userPrincipalName.Contains("_", StringComparison.OrdinalIgnoreCase) && userPrincipalName.Contains("#", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Build enhanced context information
    /// </summary>
    private async Task<EnhancedContextInfo> BuildEnhancedContext(int? tenantId, string? userRole)
    {
        var enhancedContext = new EnhancedContextInfo
        {
            UserRole = userRole
        };

        if (tenantId.HasValue)
        {
            enhancedContext.TenantId = tenantId.Value;

            // Get subscription tier
            var subscription = await _context.Subscriptions
                .Where(s => s.TenantId == tenantId.Value)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription != null)
            {
                enhancedContext.SubscriptionTier = subscription.Tier;
            }
        }

        return enhancedContext;
    }

    /// <summary>
    /// Build context JSON for logging
    /// </summary>
    private string? BuildContextJson(AiContextInfo? context, EnhancedContextInfo? enhancedContext)
    {
        var combinedContext = new
        {
            basic = context,
            enhanced = new
            {
                enhancedContext?.TenantId,
                enhancedContext?.UserRole,
                enhancedContext?.SubscriptionTier,
                enhancedContext?.UserPermissionLevel,
                enhancedContext?.CurrentScreen
            }
        };

        return JsonSerializer.Serialize(combinedContext);
    }

    private async Task<List<(string role, string content)>> GetConversationHistory(string conversationId, int maxMessages)
    {
        var logs = await _context.AiConversationLogs
            .Where(l => l.ConversationId == conversationId)
            .OrderByDescending(l => l.Timestamp)
            .Take(maxMessages)
            .OrderBy(l => l.Timestamp)
            .Select(l => new { l.UserPrompt, l.AssistantResponse })
            .ToListAsync();

        var history = new List<(string role, string content)>();
        foreach (var log in logs)
        {
            history.Add(("user", log.UserPrompt));
            history.Add(("assistant", log.AssistantResponse));
        }

        return history;
    }

    private AiSettingsDto MapToDto(AiSettingsEntity entity)
    {
        return new AiSettingsDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            IsEnabled = entity.IsEnabled,
            MarketingModeEnabled = entity.MarketingModeEnabled,
            InProductModeEnabled = entity.InProductModeEnabled,
            MaxRequestsPerHour = entity.MaxRequestsPerHour,
            MaxTokensPerRequest = entity.MaxTokensPerRequest,
            MonthlyTokenBudget = entity.MonthlyTokenBudget,
            TokensUsedThisMonth = entity.TokensUsedThisMonth,
            LastMonthlyReset = entity.LastMonthlyReset,
            ShowDisclaimer = entity.ShowDisclaimer,
            CustomSystemPrompt = entity.CustomSystemPrompt
        };
    }
}
