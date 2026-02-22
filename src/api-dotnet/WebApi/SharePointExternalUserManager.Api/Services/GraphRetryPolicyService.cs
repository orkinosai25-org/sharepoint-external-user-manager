using Microsoft.Graph.Models.ODataErrors;
using Polly;
using Polly.Retry;
using System.Net;

namespace SharePointExternalUserManager.Api.Services;

/// <summary>
/// Service interface for Graph API retry policies
/// </summary>
public interface IGraphRetryPolicyService
{
    /// <summary>
    /// Execute an async operation with retry logic for transient Graph API failures
    /// </summary>
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);

    /// <summary>
    /// Execute an async operation with retry logic for transient Graph API failures
    /// </summary>
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName);
}

/// <summary>
/// Service for handling Graph API retry logic with exponential backoff
/// Implements resilient error handling for transient failures, expired tokens, and throttling
/// </summary>
public class GraphRetryPolicyService : IGraphRetryPolicyService
{
    private readonly ILogger<GraphRetryPolicyService> _logger;
    private readonly ResiliencePipeline<object> _retryPipeline;

    public GraphRetryPolicyService(
        ILogger<GraphRetryPolicyService> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        // Load retry configuration from appsettings
        var retryConfig = configuration.GetSection("GraphRetryPolicy").Get<Models.GraphRetryConfiguration>()
            ?? new Models.GraphRetryConfiguration();

        // Build resilience pipeline with retry strategy
        _retryPipeline = new ResiliencePipelineBuilder<object>()
            .AddRetry(new RetryStrategyOptions<object>
            {
                MaxRetryAttempts = retryConfig.MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(retryConfig.InitialRetryDelaySeconds),
                MaxDelay = TimeSpan.FromSeconds(retryConfig.MaxRetryDelaySeconds),
                BackoffType = retryConfig.UseExponentialBackoff 
                    ? DelayBackoffType.Exponential 
                    : DelayBackoffType.Linear,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<object>()
                    .Handle<ODataError>(IsTransientError)
                    .Handle<HttpRequestException>(ex => IsTransientHttpError(ex))
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception;
                    _logger.LogWarning(
                        exception,
                        "Graph API request failed (attempt {AttemptNumber}/{MaxAttempts}). Retrying after {Delay}ms. Error: {ErrorMessage}",
                        args.AttemptNumber,
                        retryConfig.MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds,
                        GetErrorMessage(exception));

                    return default;
                }
            })
            .Build();
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            _logger.LogDebug("Executing Graph API operation: {OperationName}", operationName);

            var result = await _retryPipeline.ExecuteAsync(
                async cancellationToken =>
                {
                    var value = await operation();
                    return (object)value!;
                },
                CancellationToken.None);

            return (T)result;
        }
        catch (ODataError odataError)
        {
            _logger.LogError(
                odataError,
                "Graph API operation '{OperationName}' failed after all retry attempts. Error code: {ErrorCode}",
                operationName,
                odataError.Error?.Code);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Graph API operation '{OperationName}' failed after all retry attempts",
                operationName);
            throw;
        }
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return (object?)null;
        }, operationName);
    }

    /// <summary>
    /// Determines if a Graph API OData error is transient and should be retried
    /// </summary>
    private bool IsTransientError(ODataError error)
    {
        if (error.Error == null)
        {
            return false;
        }

        var errorCode = error.Error.Code?.ToLowerInvariant() ?? "";

        // Transient error codes that should be retried
        var transientErrorCodes = new[]
        {
            "servicenotavailable",          // Service temporarily unavailable
            "timeout",                       // Request timeout
            "activitylimitreached",         // Rate limiting
            "generalexception",             // General transient exception
            "requesttimeout",               // Request timeout
            "serviceunavailable",           // Service unavailable
            "throttledrequest",             // Throttling
            "tokenunavailable",             // Token refresh needed
            "unauthenticated",              // Token expired
            "invalidauthenticationtoken",   // Token expired or invalid
            "authenticationcanceled",       // Auth flow interrupted
        };

        var isTransient = transientErrorCodes.Any(code => errorCode.Contains(code));

        if (isTransient)
        {
            _logger.LogWarning(
                "Detected transient Graph API error: {ErrorCode} - {ErrorMessage}",
                error.Error.Code,
                error.Error.Message);
        }
        else
        {
            _logger.LogWarning(
                "Detected non-transient Graph API error: {ErrorCode} - {ErrorMessage}. This will not be retried.",
                error.Error.Code,
                error.Error.Message);
        }

        return isTransient;
    }

    /// <summary>
    /// Determines if an HTTP exception is transient
    /// </summary>
    private bool IsTransientHttpError(HttpRequestException ex)
    {
        if (ex.StatusCode == null)
        {
            return true; // Network errors without status code are often transient
        }

        // Transient HTTP status codes
        return ex.StatusCode switch
        {
            HttpStatusCode.RequestTimeout => true,           // 408
            HttpStatusCode.TooManyRequests => true,          // 429
            HttpStatusCode.InternalServerError => true,      // 500
            HttpStatusCode.BadGateway => true,               // 502
            HttpStatusCode.ServiceUnavailable => true,       // 503
            HttpStatusCode.GatewayTimeout => true,           // 504
            _ => false
        };
    }

    /// <summary>
    /// Extracts a readable error message from an exception
    /// </summary>
    private static string GetErrorMessage(Exception? exception)
    {
        if (exception == null)
        {
            return "Unknown error";
        }

        if (exception is ODataError odataError)
        {
            return $"{odataError.Error?.Code}: {odataError.Error?.Message}";
        }

        return exception.Message;
    }
}
