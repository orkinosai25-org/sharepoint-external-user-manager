using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.IntegrationTests.Fixtures;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;
using Xunit;

namespace SharePointExternalUserManager.Api.IntegrationTests.Tests;

/// <summary>
/// Integration tests for tenant onboarding workflow
/// Tests the complete flow: consent -> registration -> role assignment
/// </summary>
public class TenantOnboardingIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ApplicationDbContext _dbContext;

    public TenantOnboardingIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Get a scoped DbContext for assertions
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task GetConsentUrl_ReturnsValidConsentUrl()
    {
        // Act
        var response = await _client.GetAsync("/Consent/consent-url");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, string>>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("consentUrl"));
        Assert.Contains("login.microsoftonline.com", result.Data["consentUrl"]);
        Assert.Contains("adminconsent", result.Data["consentUrl"]);
    }

    [Fact]
    public async Task ConsentCallback_WithValidConsent_CreatesOrUpdatesTenant()
    {
        // Arrange
        var tenantId = $"test-tenant-{Guid.NewGuid()}";

        // Act - Simulate successful consent callback
        var response = await _client.GetAsync($"/Consent/callback?admin_consent=True&tenant={tenantId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ConsentCallback_WithError_ReturnsBadRequest()
    {
        // Act - Simulate failed consent
        var response = await _client.GetAsync("/Consent/callback?error=access_denied&error_description=User+cancelled+consent");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Equal("access_denied", result.Error.Code);
    }

    [Fact]
    public async Task TenantRegistration_WithValidData_CreatesCompleteSetup()
    {
        // Arrange
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var userId = $"test-user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@test.com";

        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = userEmail
        };

        // Act
        var response = await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(result);
        Assert.True(result.Success);

        // Verify tenant was created in database
        var tenant = _dbContext.Tenants.FirstOrDefault(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(tenant);
        Assert.Equal("Test Organization", tenant.OrganizationName);
        Assert.Equal(userEmail, tenant.PrimaryAdminEmail);
        Assert.Equal("Active", tenant.Status);

        // Verify subscription was created with trial
        var subscription = _dbContext.Subscriptions.FirstOrDefault(s => s.TenantId == tenant.Id);
        Assert.NotNull(subscription);
        Assert.Equal("Professional", subscription.Tier);
        Assert.Equal("Trial", subscription.Status);
        Assert.True(subscription.TrialExpiry > DateTime.UtcNow);

        // Verify admin user was created with TenantOwner role
        var tenantUser = _dbContext.TenantUsers.FirstOrDefault(u => u.TenantId == tenant.Id && u.AzureAdObjectId == userId);
        Assert.NotNull(tenantUser);
        Assert.Equal(userEmail, tenantUser.UserPrincipalName);
        Assert.Equal(TenantRole.TenantOwner, tenantUser.Role);
        Assert.True(tenantUser.IsActive);
    }

    [Fact]
    public async Task TenantRegistration_DuplicateTenant_ReturnsConflict()
    {
        // Arrange - First registration
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var userId = $"test-user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@test.com";

        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = userEmail
        };

        // First registration
        var firstResponse = await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        // Act - Try to register again
        var authClient2 = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var secondResponse = await authClient2.PostAsJsonAsync("/Tenants/register", registrationRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        
        var result = await secondResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Equal("TENANT_ALREADY_EXISTS", result.Error.Code);
    }

    [Fact]
    public async Task GetMe_AfterRegistration_ReturnsCompleteProfile()
    {
        // Arrange - Register a tenant first
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var userId = $"test-user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@test.com";

        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Test Organization",
            PrimaryAdminEmail = userEmail
        };

        await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);

        // Act - Get current tenant context
        var response = await authClient.GetAsync("/Tenants/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, object>>>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("tenantId"));
        Assert.True(result.Data.ContainsKey("subscriptionTier"));
        Assert.True(result.Data.ContainsKey("organizationName"));
    }

    [Fact]
    public async Task CompleteOnboardingFlow_Success()
    {
        // This test simulates the complete onboarding flow
        
        // Step 1: Get consent URL
        var consentResponse = await _client.GetAsync("/Consent/consent-url");
        Assert.Equal(HttpStatusCode.OK, consentResponse.StatusCode);

        // Step 2: Simulate admin consent (callback)
        var tenantId = $"test-tenant-{Guid.NewGuid()}";
        var callbackResponse = await _client.GetAsync($"/Consent/callback?admin_consent=True&tenant={tenantId}");
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        // Step 3: Register the tenant
        var userId = $"test-user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@test.com";

        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Complete Flow Test Org",
            PrimaryAdminEmail = userEmail
        };

        var registerResponse = await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Step 4: Verify we can access tenant data
        var meResponse = await authClient.GetAsync("/Tenants/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        // Verify complete database state
        var tenant = _dbContext.Tenants.FirstOrDefault(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(tenant);
        
        var subscription = _dbContext.Subscriptions.FirstOrDefault(s => s.TenantId == tenant.Id);
        Assert.NotNull(subscription);
        Assert.Equal("Trial", subscription.Status);
        
        var tenantUser = _dbContext.TenantUsers.FirstOrDefault(u => u.TenantId == tenant.Id);
        Assert.NotNull(tenantUser);
        Assert.Equal(TenantRole.TenantOwner, tenantUser.Role);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _dbContext?.Dispose();
    }
}
