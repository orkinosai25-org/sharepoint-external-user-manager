using System.Net;
using System.Text.Json;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantId = context.User?.FindFirst("tid")?.Value ?? "anonymous";
        var userId = context.User?.FindFirst("oid")?.Value ?? "anonymous";

        // Log the exception with correlation ID and tenant context
        _logger.LogError(
            exception,
            "Unhandled exception occurred. CorrelationId: {CorrelationId}, TenantId: {TenantId}, UserId: {UserId}, Path: {Path}",
            correlationId,
            tenantId,
            userId,
            context.Request.Path);

        // Determine status code and error type
        var (statusCode, errorCode, message) = MapExceptionToResponse(exception);

        // Build error response
        var errorResponse = new ErrorResponse
        {
            Error = errorCode,
            Message = message,
            CorrelationId = correlationId
        };

        // Include stack trace in development
        if (_environment.IsDevelopment())
        {
            errorResponse.Details = exception.ToString();
        }

        // Set response headers
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Serialize and write response
        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private (HttpStatusCode StatusCode, string ErrorCode, string Message) MapExceptionToResponse(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException _ => (
                HttpStatusCode.Forbidden,
                "ACCESS_DENIED",
                "You do not have permission to access this resource."
            ),
            InvalidOperationException ex when ex.Message.Contains("limit") || ex.Message.Contains("exceeded") => (
                HttpStatusCode.Forbidden,
                "PLAN_LIMIT_EXCEEDED",
                ex.Message
            ),
            KeyNotFoundException _ => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                "The requested resource was not found."
            ),
            ArgumentException ex => (
                HttpStatusCode.BadRequest,
                "INVALID_INPUT",
                ex.Message
            ),
            ArgumentNullException ex => (
                HttpStatusCode.BadRequest,
                "INVALID_INPUT",
                $"Required parameter missing: {ex.ParamName}"
            ),
            NotImplementedException _ => (
                HttpStatusCode.NotImplemented,
                "NOT_IMPLEMENTED",
                "This feature is not yet implemented."
            ),
            TimeoutException _ => (
                HttpStatusCode.RequestTimeout,
                "TIMEOUT",
                "The request timed out. Please try again."
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later."
            )
        };
    }

    /// <summary>
    /// Error response model
    /// </summary>
    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}

/// <summary>
/// Extension methods for registering the global exception middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// Register the global exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
