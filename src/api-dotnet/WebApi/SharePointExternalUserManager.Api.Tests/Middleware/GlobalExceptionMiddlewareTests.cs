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

        // By default, assume production environment
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegate()
    {
        // Arrange
        var nextDelegateCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextDelegateCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.True(nextDelegateCalled);
        Assert.Equal(200, _httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns403WithErrorResponse()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new UnauthorizedAccessException("Access denied");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(403, _httpContext.Response.StatusCode);
        Assert.Equal("application/json", _httpContext.Response.ContentType);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"ACCESS_DENIED\"", responseBody);
        Assert.Contains("\"message\":\"You do not have permission to access this resource.\"", responseBody);
        Assert.Contains("\"correlationId\":", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_PlanLimitException_Returns403WithPlanLimitError()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidOperationException("Upgrade to Pro to create more Client Spaces. Plan limit exceeded.");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(403, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"PLAN_LIMIT_EXCEEDED\"", responseBody);
        Assert.Contains("limit", responseBody.ToLower());
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new KeyNotFoundException("Resource not found");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(404, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"NOT_FOUND\"", responseBody);
        Assert.Contains("\"message\":\"The requested resource was not found.\"", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_Returns400WithParamName()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ArgumentNullException("clientId", "Client ID is required");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(400, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"INVALID_INPUT\"", responseBody);
        Assert.Contains("clientId", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new ArgumentException("Invalid argument provided");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(400, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"INVALID_INPUT\"", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_NotImplementedException_Returns501()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new NotImplementedException("Feature not yet implemented");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(501, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"NOT_IMPLEMENTED\"", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_TimeoutException_Returns408()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new TimeoutException("Request timed out");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(408, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"TIMEOUT\"", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500WithInternalError()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new InvalidCastException("Something went wrong");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal(500, _httpContext.Response.StatusCode);

        var responseBody = await GetResponseBody();
        Assert.Contains("\"error\":\"INTERNAL_ERROR\"", responseBody);
        Assert.Contains("\"message\":\"An unexpected error occurred. Please try again later.\"", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_LogsExceptionWithCorrelationId()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CorrelationId")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_LogsExceptionWithTenantId()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TenantId")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AnonymousUser_LogsAnonymous()
    {
        // Arrange
        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext ctx) => throw exception;

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        // Don't set up authenticated user - leave as anonymous

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("anonymous")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentEnvironment_IncludesStackTrace()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception with stack trace");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        Assert.Contains("\"details\":", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_ProductionEnvironment_DoesNotIncludeStackTrace()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var json = JsonDocument.Parse(responseBody);
        
        // In production, details should be null or not contain stack trace
        if (json.RootElement.TryGetProperty("details", out var detailsElement))
        {
            Assert.True(detailsElement.ValueKind == JsonValueKind.Null, "Details should be null in production");
        }
    }

    [Fact]
    public async Task InvokeAsync_ResponseHasCorrelationId()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        var responseBody = await GetResponseBody();
        var json = JsonDocument.Parse(responseBody);
        
        Assert.True(json.RootElement.TryGetProperty("correlationId", out var correlationIdElement));
        var correlationId = correlationIdElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_ResponseContentTypeIsJson()
    {
        // Arrange
        RequestDelegate next = (HttpContext ctx) =>
        {
            throw new Exception("Test exception");
        };

        var middleware = new GlobalExceptionMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);
        SetupAuthenticatedUser();

        // Act
        await middleware.InvokeAsync(_httpContext);

        // Assert
        Assert.Equal("application/json", _httpContext.Response.ContentType);
    }

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim("tid", "test-tenant-id"),
            new Claim("oid", "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
    }

    private async Task<string> GetResponseBody()
    {
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
