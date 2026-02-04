using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Functions.Middleware;

public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        
        if (requestData != null)
        {
            try
            {
                // Extract and validate JWT token
                var token = ExtractToken(requestData);
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No authentication token provided");
                    await WriteUnauthorizedResponse(requestData);
                    return;
                }

                // Validate token and extract claims
                var principal = await ValidateTokenAsync(token);
                
                if (principal == null)
                {
                    _logger.LogWarning("Token validation failed");
                    await WriteUnauthorizedResponse(requestData);
                    return;
                }

                // Store claims in context for downstream use
                context.Items["ClaimsPrincipal"] = principal;
                context.Items["TenantId"] = principal.FindFirst("tid")?.Value;
                context.Items["UserId"] = principal.FindFirst("oid")?.Value;
                context.Items["UserPrincipalName"] = principal.FindFirst("upn")?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication middleware error");
                await WriteUnauthorizedResponse(requestData);
                return;
            }
        }

        await next(context);
    }

    private string? ExtractToken(HttpRequestData request)
    {
        if (request.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var authHeader = authHeaders.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
        }
        return null;
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            // In production, these would come from configuration/Key Vault
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://login.microsoftonline.com/{tenant_id}/v2.0", // TODO: Replace with actual tenant ID
                
                ValidateAudience = true,
                ValidAudience = Environment.GetEnvironmentVariable("EntraId__ClientId"),
                
                ValidateIssuerSigningKey = true,
                // In production, fetch signing keys from Azure AD
                // IssuerSigningKeyResolver = ...
                
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            
            return await Task.FromResult(principal);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private async Task WriteUnauthorizedResponse(HttpRequestData request)
    {
        var response = request.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(
            "UNAUTHORIZED",
            "Invalid or missing authentication token"
        ));
        
        // Set the response on the context
        var context = request.FunctionContext;
        context.GetInvocationResult().Value = response;
    }
}
