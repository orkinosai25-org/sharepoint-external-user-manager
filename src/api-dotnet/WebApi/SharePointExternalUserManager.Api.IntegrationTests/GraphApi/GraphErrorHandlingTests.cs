using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph.Models.ODataErrors;
using Xunit;
using SharePointExternalUserManager.Api.Services;

namespace SharePointExternalUserManager.Api.IntegrationTests.GraphApi;

/// <summary>
/// Integration tests for Graph API error handling and retry logic
/// Tests response to 401 (Unauthorized), 403 (Forbidden), 404 (Not Found), 429 (Rate Limited), 503 (Service Unavailable)
/// </summary>
public class GraphErrorHandlingTests
{
    private readonly IGraphRetryPolicyService _retryPolicyService;

    public GraphErrorHandlingTests()
    {
        // Create test configuration for retry policy
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GraphRetryPolicy:MaxRetryAttempts"] = "3",
            ["GraphRetryPolicy:InitialRetryDelaySeconds"] = "1",
            ["GraphRetryPolicy:MaxRetryDelaySeconds"] = "5",
            ["GraphRetryPolicy:UseExponentialBackoff"] = "true",
            ["GraphRetryPolicy:JitterFactor"] = "0.2",
            ["GraphRetryPolicy:RequestTimeoutSeconds"] = "30"
        });
        var configuration = configBuilder.Build();

        _retryPolicyService = new GraphRetryPolicyService(
            new NullLogger<GraphRetryPolicyService>(),
            configuration);
    }

    [Fact]
    public async Task GraphRetry_401UnauthorizedError_ThrowsODataError()
    {
        // Arrange - simulate 401 Unauthorized
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "Unauthorized",
                Message = "Access token has expired or is invalid"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                await Task.CompletedTask;
                throw odataError;
            }, "Test401Operation");
        });
    }

    [Fact]
    public async Task GraphRetry_403ForbiddenError_ThrowsODataError()
    {
        // Arrange - simulate 403 Forbidden (insufficient permissions)
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "Forbidden",
                Message = "Insufficient privileges to complete the operation"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                await Task.CompletedTask;
                throw odataError;
            }, "Test403Operation");
        });
    }

    [Fact]
    public async Task GraphRetry_404NotFoundError_ThrowsODataError()
    {
        // Arrange - simulate 404 Not Found
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "NotFound",
                Message = "The requested resource was not found"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                await Task.CompletedTask;
                throw odataError;
            }, "Test404Operation");
        });
    }

    [Fact]
    public async Task GraphRetry_429RateLimitError_RetriesWithBackoff()
    {
        // Arrange - simulate 429 Too Many Requests (rate limiting)
        var attemptCount = 0;
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "TooManyRequests",
                Message = "Rate limit exceeded"
            }
        };

        // Act
        try
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                
                // First 2 attempts fail with rate limit, 3rd succeeds
                if (attemptCount < 3)
                {
                    throw odataError;
                }
            }, "Test429Operation");
        }
        catch
        {
            // Expected to eventually succeed or exhaust retries
        }

        // Assert - should have retried at least twice
        Assert.True(attemptCount >= 2, $"Expected multiple retry attempts, but got {attemptCount}");
    }

    [Fact]
    public async Task GraphRetry_ServiceNotAvailableError_Retries()
    {
        // Arrange - simulate transient 503 Service Unavailable
        var attemptCount = 0;
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "ServiceNotAvailable",
                Message = "Service temporarily unavailable"
            }
        };

        // Act
        var result = await _retryPolicyService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            // First attempt fails, second succeeds
            if (attemptCount == 1)
            {
                throw odataError;
            }
            
            return "Success";
        }, "Test503Operation");

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task GraphRetry_TimeoutError_RetriesOperation()
    {
        // Arrange
        var attemptCount = 0;

        // Act
        var result = await _retryPolicyService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            // First attempt times out, second succeeds
            if (attemptCount == 1)
            {
                throw new TimeoutException("Request timed out");
            }
            
            return "Success";
        }, "TestTimeoutOperation");

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task GraphRetry_InvalidAuthenticationTokenError_Retries()
    {
        // Arrange - simulate token expiration
        var attemptCount = 0;
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "InvalidAuthenticationToken",
                Message = "Access token has expired"
            }
        };

        // Act
        var result = await _retryPolicyService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            // First attempt fails with expired token, second succeeds (assumes token refresh)
            if (attemptCount == 1)
            {
                throw odataError;
            }
            
            return "Success";
        }, "TestTokenExpirationOperation");

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task GraphRetry_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange - operation that consistently fails
        var attemptCount = 0;
        var odataError = new ODataError
        {
            Error = new MainError
            {
                Code = "ServiceNotAvailable",
                Message = "Service temporarily unavailable"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
        {
            await _retryPolicyService.ExecuteWithRetryAsync(async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                // Always fail
                throw odataError;
            }, "TestMaxRetriesOperation");
        });

        // Should have attempted multiple times before giving up (MaxRetryAttempts = 3, so initial + 3 retries = 4 total)
        Assert.True(attemptCount > 1, $"Expected multiple retry attempts, but got {attemptCount}");
    }

    [Fact]
    public async Task GraphRetry_HttpRequestException_Retries()
    {
        // Arrange - simulate network error
        var attemptCount = 0;

        // Act
        var result = await _retryPolicyService.ExecuteWithRetryAsync(async () =>
        {
            attemptCount++;
            await Task.CompletedTask;
            
            // First attempt fails with network error, second succeeds
            if (attemptCount == 1)
            {
                throw new HttpRequestException("Network connection failed");
            }
            
            return "Success";
        }, "TestHttpRequestException");

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, attemptCount);
    }
}
