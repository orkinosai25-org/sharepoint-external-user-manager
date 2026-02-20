using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;
using SharePointExternalUserManager.Api.Middleware;

namespace SharePointExternalUserManager.Api.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _mockLogger;
    private readonly Mock<IHostEnvironment> _mockEnvironment;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionMiddleware>>();
        _mockEnvironment = new Mock<IHostEnvironment>();
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegate()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(200, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_ReturnsInternalError()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(500, _httpContext.Response.StatusCode);
        Assert.Equal("application/json", _httpContext.Response.ContentType);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("INTERNAL_ERROR", errorResponse.Error);
        Assert.Equal("An unexpected error occurred. Please try again later.", errorResponse.Message);
        Assert.NotEmpty(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns403Forbidden()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(403, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("ACCESS_DENIED", errorResponse.Error);
        Assert.Equal("You do not have permission to access this resource.", errorResponse.Message);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404NotFound()
    {
        // Arrange
        var exception = new KeyNotFoundException("Resource not found");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(404, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("NOT_FOUND", errorResponse.Error);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_Returns400BadRequest()
    {
        // Arrange
        var exception = new ArgumentNullException("testParam", "Parameter cannot be null");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(400, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("INVALID_INPUT", errorResponse.Error);
        Assert.Contains("testParam", errorResponse.Message);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(400, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("INVALID_INPUT", errorResponse.Error);
        Assert.Equal("Invalid argument", errorResponse.Message);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationWithLimit_ReturnsPlanLimitExceeded()
    {
        // Arrange
        var exception = new InvalidOperationException("Client limit exceeded for your plan");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(403, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("PLAN_LIMIT_EXCEEDED", errorResponse.Error);
        Assert.Contains("limit", errorResponse.Message.ToLower());
    }

    [Fact]
    public async Task InvokeAsync_NotImplementedException_Returns501NotImplemented()
    {
        // Arrange
        var exception = new NotImplementedException("Feature not implemented");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(501, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("NOT_IMPLEMENTED", errorResponse.Error);
    }

    [Fact]
    public async Task InvokeAsync_TimeoutException_Returns408RequestTimeout()
    {
        // Arrange
        var exception = new TimeoutException("Request timed out");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(408, _httpContext.Response.StatusCode);
        
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Equal("TIMEOUT", errorResponse.Error);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticatedUser_LogsTenantIdAndUserId()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var userId = "test-user-id";
        
        var claims = new List<Claim>
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("CorrelationId:") && 
                    v.ToString()!.Contains($"TenantId: {tenantId}") && 
                    v.ToString()!.Contains($"UserId: {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAnonymousUser_LogsAnonymousTenantId()
    {
        // Arrange (no claims set - anonymous user)
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("TenantId: anonymous") && 
                    v.ToString()!.Contains("UserId: anonymous")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_IncludesStackTrace()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        
        var exception = new Exception("Test exception with stack trace");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Details);
        Assert.Contains("Test exception with stack trace", errorResponse.Details);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_ExcludesStackTrace()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Null(errorResponse.Details);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysGeneratesCorrelationId()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.CorrelationId);
        Assert.NotEmpty(errorResponse.CorrelationId);
        
        // Verify it's a valid GUID format
        Assert.True(Guid.TryParse(errorResponse.CorrelationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_ResponseFormat_IsCamelCase()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        
        // Check that response contains camelCase properties
        Assert.Contains("\"error\":", responseBody);
        Assert.Contains("\"message\":", responseBody);
        Assert.Contains("\"correlationId\":", responseBody);
        
        // Should not contain PascalCase
        Assert.DoesNotContain("\"Error\":", responseBody);
        Assert.DoesNotContain("\"Message\":", responseBody);
        Assert.DoesNotContain("\"CorrelationId\":", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_LogsExceptionWithRequestPath()
    {
        // Arrange
        _httpContext.Request.Path = "/api/clients/123";
        
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Path: /api/clients/123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private async Task<string> GetResponseBody()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
