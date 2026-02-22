using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using SharePointExternalUserManager.Api.Services;
using System.Net;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class GraphRetryPolicyServiceTests
{
    private readonly Mock<ILogger<GraphRetryPolicyService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly GraphRetryPolicyService _service;

    public GraphRetryPolicyServiceTests()
    {
        _loggerMock = new Mock<ILogger<GraphRetryPolicyService>>();

        // Create test configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["GraphRetryPolicy:MaxRetryAttempts"] = "3",
            ["GraphRetryPolicy:InitialRetryDelaySeconds"] = "1",
            ["GraphRetryPolicy:MaxRetryDelaySeconds"] = "5",
            ["GraphRetryPolicy:UseExponentialBackoff"] = "true",
            ["GraphRetryPolicy:JitterFactor"] = "0.2",
            ["GraphRetryPolicy:RequestTimeoutSeconds"] = "30"
        }!);
        _configuration = configBuilder.Build();

        _service = new GraphRetryPolicyService(_loggerMock.Object, _configuration);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = "test-result";
        var operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TransientError_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 3)
            {
                // Simulate transient error
                var odataError = new ODataError
                {
                    Error = new MainError
                    {
                        Code = "ServiceUnavailable",
                        Message = "Service temporarily unavailable"
                    }
                };
                throw odataError;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_PermanentError_FailsImmediately()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            var odataError = new ODataError
            {
                Error = new MainError
                {
                    Code = "itemNotFound",
                    Message = "Item not found"
                }
            };
            throw odataError;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(
            () => _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        // Should fail immediately without retries for non-transient errors
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TokenExpired_RetriesOperation()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount == 1)
            {
                var odataError = new ODataError
                {
                    Error = new MainError
                    {
                        Code = "InvalidAuthenticationToken",
                        Message = "Access token has expired"
                    }
                };
                throw odataError;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RateLimited_RetriesWithBackoff()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                var odataError = new ODataError
                {
                    Error = new MainError
                    {
                        Code = "ActivityLimitReached",
                        Message = "Too many requests"
                    }
                };
                throw odataError;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_HttpRequestException_Transient_Retries()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new HttpRequestException("Service unavailable", null, HttpStatusCode.ServiceUnavailable);
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TimeoutException_Retries()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new TimeoutException("Request timed out");
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            var odataError = new ODataError
            {
                Error = new MainError
                {
                    Code = "ServiceUnavailable",
                    Message = "Service temporarily unavailable"
                }
            };
            throw odataError;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(
            () => _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        // Should attempt initial call + max retries (1 + 3 = 4)
        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_ExecutesSuccessfully()
    {
        // Arrange
        var executed = false;
        var operation = () =>
        {
            executed = true;
            return Task.CompletedTask;
        };

        // Act
        await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_VoidOperation_RetriesOnTransientError()
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount < 2)
            {
                var odataError = new ODataError
                {
                    Error = new MainError
                    {
                        Code = "Timeout",
                        Message = "Request timeout"
                    }
                };
                throw odataError;
            }
            return Task.CompletedTask;
        };

        // Act
        await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData("ServiceNotAvailable")]
    [InlineData("Timeout")]
    [InlineData("ActivityLimitReached")]
    [InlineData("GeneralException")]
    [InlineData("RequestTimeout")]
    [InlineData("ServiceUnavailable")]
    [InlineData("ThrottledRequest")]
    [InlineData("TokenUnavailable")]
    [InlineData("Unauthenticated")]
    [InlineData("InvalidAuthenticationToken")]
    public async Task ExecuteWithRetryAsync_TransientErrorCodes_Retries(string errorCode)
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            if (callCount == 1)
            {
                var odataError = new ODataError
                {
                    Error = new MainError
                    {
                        Code = errorCode,
                        Message = "Transient error"
                    }
                };
                throw odataError;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData("itemNotFound")]
    [InlineData("ResourceNotFound")]
    [InlineData("accessDenied")]
    [InlineData("Forbidden")]
    [InlineData("invalidRequest")]
    [InlineData("BadRequest")]
    public async Task ExecuteWithRetryAsync_NonTransientErrorCodes_DoesNotRetry(string errorCode)
    {
        // Arrange
        var callCount = 0;
        var operation = () =>
        {
            callCount++;
            var odataError = new ODataError
            {
                Error = new MainError
                {
                    Code = errorCode,
                    Message = "Permanent error"
                }
            };
            throw odataError;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(
            () => _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        // Should fail immediately without retries
        Assert.Equal(1, callCount);
    }
}
