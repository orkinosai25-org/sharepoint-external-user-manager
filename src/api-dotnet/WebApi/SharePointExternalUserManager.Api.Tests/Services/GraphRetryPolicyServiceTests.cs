using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;
using Moq;
using SharePointExternalUserManager.Api.Services;
using Xunit;

namespace SharePointExternalUserManager.Api.Tests.Services;

public class GraphRetryPolicyServiceTests
{
    private readonly Mock<ILogger<GraphRetryPolicyService>> _loggerMock;
    private readonly GraphRetryPolicyService _service;

    public GraphRetryPolicyServiceTests()
    {
        _loggerMock = new Mock<ILogger<GraphRetryPolicyService>>();
        _service = new GraphRetryPolicyService(_loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var expectedResult = "test result";
        Func<Task<string>> operation = () => Task.FromResult(expectedResult);

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessfulVoidOperation_Completes()
    {
        // Arrange
        var executed = false;
        Func<Task> operation = () =>
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
    public async Task ExecuteWithRetryAsync_TransientError429_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                // Simulate throttling on first attempt
                var error = new ODataError
                {
                    ResponseStatusCode = 429,
                    Error = new MainError { Code = "TooManyRequests" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount); // First attempt failed, second succeeded
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ServiceUnavailable503_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                // Simulate service unavailable on first attempt
                var error = new ODataError
                {
                    ResponseStatusCode = 503,
                    Error = new MainError { Code = "ServiceUnavailable" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ExpiredToken401_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                // Simulate expired token on first attempt
                var error = new ODataError
                {
                    ResponseStatusCode = 401,
                    Error = new MainError { Code = "ExpiredAuthenticationToken" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_InvalidAuthenticationToken401_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                // Simulate invalid token on first attempt (can be refreshed)
                var error = new ODataError
                {
                    ResponseStatusCode = 401,
                    Error = new MainError { Code = "InvalidAuthenticationToken" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_AccessDenied403_DoesNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            var error = new ODataError
            {
                ResponseStatusCode = 403,
                Error = new MainError { Code = "accessDenied" }
            };
            throw error;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
            await _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        Assert.Equal(1, attemptCount); // Should not retry
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_BadRequest400_DoesNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            var error = new ODataError
            {
                ResponseStatusCode = 400,
                Error = new MainError { Code = "BadRequest" }
            };
            throw error;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
            await _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        Assert.Equal(1, attemptCount); // Should not retry
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NotFound404_DoesNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            var error = new ODataError
            {
                ResponseStatusCode = 404,
                Error = new MainError { Code = "itemNotFound" }
            };
            throw error;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
            await _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        Assert.Equal(1, attemptCount); // Should not retry
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_InternalServerError500_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                // Simulate server error on first two attempts
                var error = new ODataError
                {
                    ResponseStatusCode = 500,
                    Error = new MainError { Code = "InternalServerError" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_GatewayTimeout504_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                var error = new ODataError
                {
                    ResponseStatusCode = 504,
                    Error = new MainError { Code = "GatewayTimeout" }
                };
                throw error;
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            var error = new ODataError
            {
                ResponseStatusCode = 429,
                Error = new MainError { Code = "TooManyRequests" }
            };
            throw error;
        };

        // Act & Assert
        await Assert.ThrowsAsync<ODataError>(async () =>
            await _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        // Should retry 3 times (1 initial + 3 retries = 4 total attempts)
        Assert.Equal(4, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_HttpRequestException_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new HttpRequestException("Network error");
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_TimeoutException_RetriesAndSucceeds()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                throw new TimeoutException("Request timeout");
            }
            return Task.FromResult("success");
        };

        // Act
        var result = await _service.ExecuteWithRetryAsync(operation, "TestOperation");

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_NonTransientException_DoesNotRetry()
    {
        // Arrange
        var attemptCount = 0;
        Func<Task<string>> operation = () =>
        {
            attemptCount++;
            throw new InvalidOperationException("Non-transient error");
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ExecuteWithRetryAsync(operation, "TestOperation"));

        Assert.Equal(1, attemptCount); // Should not retry non-transient exceptions
    }
}
