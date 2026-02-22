using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SharePointExternalUserManager.Api.IntegrationTests.Fixtures;

/// <summary>
/// Helper methods for creating authenticated HTTP requests in integration tests
/// </summary>
public static class TestAuthenticationHelper
{
    /// <summary>
    /// Add a test authentication token to the HTTP client
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(
        HttpClient client,
        string tenantId = "test-tenant-id",
        string userId = "test-user-id",
        string userPrincipalName = "testuser@test.com",
        string email = "testuser@test.com")
    {
        var claims = new[]
        {
            new Claim("tid", tenantId),
            new Claim("oid", userId),
            new Claim("upn", userPrincipalName),
            new Claim("email", email)
        };

        // Create a simple token representation (not a real JWT, just for testing)
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join(",", claims.Select(c => $"{c.Type}:{c.Value}"))));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}

/// <summary>
/// Test authentication handler for integration tests
/// Allows tests to bypass real authentication while still validating auth logic
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Decode the test token
            var token = authHeader["Bearer ".Length..];
            var claimData = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var claimPairs = claimData.Split(',');

            var claims = new List<Claim>();
            foreach (var pair in claimPairs)
            {
                var parts = pair.Split(':');
                if (parts.Length == 2)
                {
                    claims.Add(new Claim(parts[0], parts[1]));
                }
            }

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token"));
        }
    }
}
