using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.Data.Entities;
using SharePointExternalUserManager.Functions.Models;

namespace SharePointExternalUserManager.Api.IntegrationTests.Workflows;

/// <summary>
/// Integration tests for tenant onboarding workflow
/// Tests the complete flow: consent URL generation -> admin consent -> tenant creation
/// </summary>
public class TenantOnboardingWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TenantOnboardingWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TenantOnboarding_CompleteFlow_Success()
    {
        // Step 1: Get consent URL
        var consentResponse = await _client.GetAsync("/Consent/url?redirectUri=https://app.example.com/callback");
        
        Assert.Equal(HttpStatusCode.OK, consentResponse.StatusCode);
        var consentData = await consentResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(consentData);
        Assert.True(consentData.Success);

        // Step 2: Simulate admin consent callback
        var tenantId = "test-tenant-onboarding-" + Guid.NewGuid().ToString();
        var callbackResponse = await _client.GetAsync(
            $"/Consent/callback?admin_consent=True&tenant={tenantId}");
        
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);
        var callbackData = await callbackResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(callbackData);
        Assert.True(callbackData.Success);

        // Step 3: Verify tenant was created in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        
        Assert.NotNull(tenant);
        Assert.Equal("Active", tenant.Status);
    }

    [Fact]
    public async Task TenantOnboarding_ConsentDenied_ReturnsError()
    {
        // Simulate user denying consent
        var response = await _client.GetAsync(
            "/Consent/callback?error=access_denied&error_description=User%20cancelled");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(data);
        Assert.False(data.Success);
        Assert.Equal("access_denied", data.Error?.Code);
    }

    [Fact]
    public async Task TenantOnboarding_DuplicateTenant_UpdatesExisting()
    {
        var tenantId = "test-tenant-duplicate-" + Guid.NewGuid().ToString();

        // First consent
        var firstResponse = await _client.GetAsync(
            $"/Consent/callback?admin_consent=True&tenant={tenantId}");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        // Second consent with same tenant ID
        var secondResponse = await _client.GetAsync(
            $"/Consent/callback?admin_consent=True&tenant={tenantId}");
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        // Verify only one tenant exists
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var tenantCount = await db.Tenants
            .CountAsync(t => t.EntraIdTenantId == tenantId);
        
        Assert.Equal(1, tenantCount);
    }

    [Fact]
    public async Task TenantOnboarding_MissingTenantId_ReturnsError()
    {
        // Admin consent without tenant ID
        var response = await _client.GetAsync(
            "/Consent/callback?admin_consent=True");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(data);
        Assert.False(data.Success);
    }

    [Fact]
    public async Task TenantOnboarding_ConsentNotGranted_ReturnsError()
    {
        var tenantId = "test-tenant-no-consent-" + Guid.NewGuid().ToString();
        
        // Admin consent flag set to false
        var response = await _client.GetAsync(
            $"/Consent/callback?admin_consent=False&tenant={tenantId}");
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        Assert.NotNull(data);
        Assert.False(data.Success);
        Assert.Equal("INVALID_CONSENT", data.Error?.Code);
    }
}
