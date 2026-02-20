using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SharePointExternalUserManager.Api.Middleware;

/// <summary>
/// Middleware to protect Swagger endpoints with authentication in production
/// ISSUE-08: Secure Swagger in Production
/// </summary>
public class SwaggerAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SwaggerAuthorizationMiddleware> _logger;

    public SwaggerAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<SwaggerAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if request is for Swagger endpoints
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        
        if (path.StartsWith("/swagger"))
        {
            // Require authentication for Swagger in production
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning(
                    "Unauthorized Swagger access attempt from {IP}",
                    context.Connection.RemoteIpAddress);
                
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication required to access Swagger documentation"
                });
                
                return;
            }
            
            _logger.LogInformation(
                "Swagger accessed by authenticated user: {User}",
                context.User.Identity?.Name ?? "Unknown");
        }
        
        await _next(context);
    }
}

/// <summary>
/// Extension method for adding Swagger authorization middleware
/// </summary>
public static class SwaggerAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseSwaggerAuthorization(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SwaggerAuthorizationMiddleware>();
    }
}
