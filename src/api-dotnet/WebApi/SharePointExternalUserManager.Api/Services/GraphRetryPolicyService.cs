using Microsoft.Graph.Models.ODataErrors;
using Polly;
using Polly.Retry;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service that provides resilience policies for Microsoft Graph API calls
/// Handles transient failures, throttling, and token expiration scenarios
/// </summary>
public interface IGraphRetryPolicyService
{
    /// <summary>
    /// Execute an async operation with retry policy for Graph API calls
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);
    
    /// <summary>
    /// Execute an async operation without return value with retry policy
    /// </summary>
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName);
}

public class GraphRetryPolicyService : IGraphRetryPolicyService
{
    private readonly ILogger<GraphRetryPolicyService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GraphRetryPolicyService(ILogger<GraphRetryPolicyService> logger)
    {
        _logger = logger;
        
        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<ODataError>(ex => IsTransientError(ex))
            .Or<HttpRequestException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Graph API call failed. Retry {RetryCount} after {RetryDelay}s. Operation: {OperationName}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        context.OperationKey);
                });
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var context = new Context(operationName);
        
        try
        {
            return await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                _logger.LogDebug("Executing Graph API operation: {OperationName}", operationName);
                return await operation();
            }, context);
        }
        catch (ODataError odataError)
        {
            // Log final failure after all retries
            _logger.LogError(
                odataError,
                "Graph API operation failed after retries. Operation: {OperationName}, ErrorCode: {ErrorCode}",
                operationName,
                odataError.Error?.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Graph API operation failed after retries. Operation: {OperationName}",
                operationName);
            throw;
        }
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        var context = new Context(operationName);
        
        try
        {
            await _retryPolicy.ExecuteAsync(async (ctx) =>
            {
                _logger.LogDebug("Executing Graph API operation: {OperationName}", operationName);
                await operation();
            }, context);
        }
        catch (ODataError odataError)
        {
            // Log final failure after all retries
            _logger.LogError(
                odataError,
                "Graph API operation failed after retries. Operation: {OperationName}, ErrorCode: {ErrorCode}",
                operationName,
                odataError.Error?.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Graph API operation failed after retries. Operation: {OperationName}",
                operationName);
            throw;
        }
    }

    /// <summary>
    /// Determine if an ODataError represents a transient failure that should be retried
    /// </summary>
    private bool IsTransientError(ODataError error)
    {
        var statusCode = error.ResponseStatusCode;
        var errorCode = error.Error?.Code;

        if (statusCode == 0)
            return false;

        // Retry on throttling (429 Too Many Requests)
        if (statusCode == 429)
        {
            _logger.LogInformation(
                "Throttling detected (429). Will retry. ErrorCode: {ErrorCode}",
                errorCode);
            return true;
        }

        // Retry on service unavailable (503) or gateway timeout (504)
        if (statusCode == 503 || statusCode == 504)
        {
            _logger.LogInformation(
                "Service unavailable ({StatusCode}). Will retry. ErrorCode: {ErrorCode}",
                statusCode,
                errorCode);
            return true;
        }

        // Retry on expired token (401 with specific error codes)
        if (statusCode == 401)
        {
            // Specific token-related errors that can be retried
            // The token acquisition library should automatically refresh
            if (errorCode == "InvalidAuthenticationToken" || 
                errorCode == "CompactTokenValidationFailed" ||
                errorCode == "ExpiredAuthenticationToken")
            {
                _logger.LogInformation(
                    "Token error detected (401 - {ErrorCode}). Will retry to allow token refresh.",
                    errorCode);
                return true;
            }
        }

        // Don't retry on client errors (400-499) except those explicitly handled above
        if (statusCode >= 400 && statusCode < 500)
        {
            _logger.LogDebug(
                "Client error ({StatusCode} - {ErrorCode}). Will not retry.",
                statusCode,
                errorCode);
            return false;
        }

        // Retry on server errors (500-599) except 501 (Not Implemented) and 505 (HTTP Version Not Supported)
        if (statusCode >= 500 && statusCode < 600 && statusCode != 501 && statusCode != 505)
        {
            _logger.LogInformation(
                "Server error ({StatusCode}). Will retry. ErrorCode: {ErrorCode}",
                statusCode,
                errorCode);
            return true;
        }

        return false;
    }
}
