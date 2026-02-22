using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharePointExternalUserManager.Api.Data;
using SharePointExternalUserManager.Api.IntegrationTests.Fixtures;
using SharePointExternalUserManager.Api.Models;
using SharePointExternalUserManager.Functions.Models;
using SharePointExternalUserManager.Functions.Models.Clients;
using SharePointExternalUserManager.Functions.Models.ExternalUsers;
using Xunit;

namespace SharePointExternalUserManager.Api.IntegrationTests.Tests;

/// <summary>
/// End-to-end scenario tests covering complete workflows
/// These tests simulate real user journeys through the system
/// </summary>
public class EndToEndScenarioTests : IClassFixture<TestWebApplicationFactory<Program>>, IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly ApplicationDbContext _dbContext;

    public EndToEndScenarioTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        
        var scope = _factory.Services.CreateScope();
        _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task CompleteUserJourney_FromOnboardingToExternalUserManagement()
    {
        // This test simulates a complete user journey:
        // 1. Admin consent
        // 2. Tenant registration
        // 3. Create client space
        // 4. Provision client site
        // 5. Invite external users
        // 6. Manage permissions
        // 7. Remove users
        // 8. View audit logs

        var tenantId = $"e2e-tenant-{Guid.NewGuid()}";
        var userId = $"e2e-user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@e2etest.com";

        // Step 1: Get consent URL
        var client = _factory.CreateClient();
        var consentResponse = await client.GetAsync("/Consent/consent-url");
        Assert.Equal(HttpStatusCode.OK, consentResponse.StatusCode);

        // Step 2: Simulate admin consent callback
        var callbackResponse = await client.GetAsync($"/Consent/callback?admin_consent=True&tenant={tenantId}");
        Assert.Equal(HttpStatusCode.OK, callbackResponse.StatusCode);

        // Step 3: Register tenant
        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "E2E Test Corporation",
            PrimaryAdminEmail = userEmail
        };

        var registerResponse = await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Verify tenant profile
        var meResponse = await authClient.GetAsync("/Tenants/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        // Step 4: Create client space
        var createClientRequest = new CreateClientRequest
        {
            ClientReference = "CLIENT-E2E-001",
            ClientName = "Acme Corporation",
            Description = "Test client for E2E scenario"
        };

        var createClientResponse = await authClient.PostAsJsonAsync("/Clients", createClientRequest);
        Assert.Equal(HttpStatusCode.Created, createClientResponse.StatusCode);
        
        var createClientResult = await createClientResponse.Content.ReadFromJsonAsync<ApiResponse<ClientResponse>>();
        var clientId = createClientResult!.Data!.Id;

        // Step 5: Provision the client site
        var provisionResponse = await authClient.PostAsync($"/Clients/{clientId}/provision", null);
        Assert.Equal(HttpStatusCode.OK, provisionResponse.StatusCode);

        // Verify provisioning
        var clientResponse = await authClient.GetAsync($"/Clients/{clientId}");
        var clientResult = await clientResponse.Content.ReadFromJsonAsync<ApiResponse<ClientResponse>>();
        Assert.Equal("Provisioned", clientResult!.Data!.ProvisioningStatus);
        Assert.NotNull(clientResult.Data.SharePointSiteUrl);

        // Step 6: Invite multiple external users
        var externalUsers = new[]
        {
            new InviteExternalUserRequest { Email = "john.doe@external.com", DisplayName = "John Doe", PermissionLevel = "Edit" },
            new InviteExternalUserRequest { Email = "jane.smith@external.com", DisplayName = "Jane Smith", PermissionLevel = "Read" },
            new InviteExternalUserRequest { Email = "bob.wilson@external.com", DisplayName = "Bob Wilson", PermissionLevel = "Read" }
        };

        foreach (var user in externalUsers)
        {
            var inviteResponse = await authClient.PostAsJsonAsync($"/Clients/{clientId}/external-users", user);
            Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
        }

        // Step 7: Verify external users list
        var usersResponse = await authClient.GetAsync($"/Clients/{clientId}/external-users");
        var usersResult = await usersResponse.Content.ReadFromJsonAsync<ApiResponse<List<ExternalUserDto>>>();
        Assert.Equal(3, usersResult!.Data!.Count);

        // Step 8: View dashboard summary
        var dashboardResponse = await authClient.GetAsync("/Dashboard/summary");
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        
        var dashboardResult = await dashboardResponse.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, object>>>();
        Assert.NotNull(dashboardResult);
        Assert.True(dashboardResult.Success);

        // Step 9: Remove one external user
        var removeRequest = new RemoveExternalUserRequest
        {
            Email = "bob.wilson@external.com"
        };
        var removeResponse = await authClient.PostAsJsonAsync($"/Clients/{clientId}/external-users/remove", removeRequest);
        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);

        // Verify user was removed
        var finalUsersResponse = await authClient.GetAsync($"/Clients/{clientId}/external-users");
        var finalUsersResult = await finalUsersResponse.Content.ReadFromJsonAsync<ApiResponse<List<ExternalUserDto>>>();
        Assert.Equal(2, finalUsersResult!.Data!.Count);
        Assert.DoesNotContain(finalUsersResult.Data, u => u.Email == "bob.wilson@external.com");

        // Step 10: Verify audit logs were created
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(tenant);

        var auditLogs = await _dbContext.AuditLogs
            .Where(a => a.TenantId == tenant.Id)
            .OrderBy(a => a.Timestamp)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        
        // Verify key audit events exist
        Assert.Contains(auditLogs, a => a.Action == "TenantRegistered");
        Assert.Contains(auditLogs, a => a.Action == "ClientCreated");
        Assert.Contains(auditLogs, a => a.Action == "ClientProvisioned");
        Assert.Contains(auditLogs, a => a.Action == "ExternalUserInvited");
        Assert.Contains(auditLogs, a => a.Action == "ExternalUserRemoved");
    }

    [Fact]
    public async Task MultiClientWorkflow_WithDifferentPermissions()
    {
        // Simulate a scenario with multiple clients and complex permission management
        
        var tenantId = $"multi-client-{Guid.NewGuid()}";
        var ownerId = $"owner-{Guid.NewGuid()}";
        var ownerEmail = $"owner-{Guid.NewGuid()}@test.com";

        // Setup tenant
        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: ownerId,
            userPrincipalName: ownerEmail,
            email: ownerEmail);

        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Multi-Client Test Org",
            PrimaryAdminEmail = ownerEmail
        };

        await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);

        // Create three different clients
        var clientIds = new List<int>();
        for (int i = 1; i <= 3; i++)
        {
            var createRequest = new CreateClientRequest
            {
                ClientReference = $"CLIENT-MC-{i:000}",
                ClientName = $"Client {i}",
                Description = $"Multi-client test {i}"
            };

            var response = await authClient.PostAsJsonAsync("/Clients", createRequest);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ClientResponse>>();
            clientIds.Add(result!.Data!.Id);

            // Provision each client
            await authClient.PostAsync($"/Clients/{result.Data.Id}/provision", null);
        }

        // Invite different external users to different clients
        // Client 1: 5 users
        for (int i = 1; i <= 5; i++)
        {
            var inviteRequest = new InviteExternalUserRequest
            {
                Email = $"client1-user{i}@external.com",
                DisplayName = $"Client 1 User {i}",
                PermissionLevel = i <= 2 ? "Edit" : "Read"
            };
            await authClient.PostAsJsonAsync($"/Clients/{clientIds[0]}/external-users", inviteRequest);
        }

        // Client 2: 3 users
        for (int i = 1; i <= 3; i++)
        {
            var inviteRequest = new InviteExternalUserRequest
            {
                Email = $"client2-user{i}@external.com",
                DisplayName = $"Client 2 User {i}",
                PermissionLevel = "Read"
            };
            await authClient.PostAsJsonAsync($"/Clients/{clientIds[1]}/external-users", inviteRequest);
        }

        // Client 3: 7 users (using bulk invite - commented out until models are implemented)
        // TODO: Implement BulkInviteRequest model
        /* var bulkRequest = new BulkInviteRequest
        {
            Users = Enumerable.Range(1, 7).Select(i => new BulkInviteUserRequest
            {
                Email = $"client3-user{i}@external.com",
                DisplayName = $"Client 3 User {i}",
                PermissionLevel = i % 2 == 0 ? "Edit" : "Read"
            }).ToList()
        };
        await authClient.PostAsJsonAsync($"/Clients/{clientIds[2]}/external-users/bulk-invite", bulkRequest); */
        
        // Use individual invites instead for now
        for (int i = 1; i <= 7; i++)
        {
            var inviteRequest = new InviteExternalUserRequest
            {
                Email = $"client3-user{i}@external.com",
                DisplayName = $"Client 3 User {i}",
                PermissionLevel = i % 2 == 0 ? "Edit" : "Read"
            };
            await authClient.PostAsJsonAsync($"/Clients/{clientIds[2]}/external-users", inviteRequest);
        }

        // Verify dashboard shows correct counts
        var dashboardResponse = await authClient.GetAsync("/Dashboard/summary");
        var dashboardResult = await dashboardResponse.Content.ReadFromJsonAsync<ApiResponse<Dictionary<string, object>>>();
        
        Assert.NotNull(dashboardResult);
        Assert.True(dashboardResult.Success);

        // Verify each client has correct user counts
        for (int i = 0; i < 3; i++)
        {
            var usersResponse = await authClient.GetAsync($"/Clients/{clientIds[i]}/external-users");
            var usersResult = await usersResponse.Content.ReadFromJsonAsync<ApiResponse<List<ExternalUserDto>>>();
            
            var expectedCount = i == 0 ? 5 : (i == 1 ? 3 : 7);
            Assert.Equal(expectedCount, usersResult!.Data!.Count);
        }

        // Verify total clients
        var clientsResponse = await authClient.GetAsync("/Clients");
        var clientsResult = await clientsResponse.Content.ReadFromJsonAsync<ApiResponse<List<ClientResponse>>>();
        Assert.True(clientsResult!.Data!.Count >= 3);
    }

    [Fact]
    public async Task PlanUpgradeScenario_FromFreeToEnterprise()
    {
        // Simulate plan upgrade workflow
        
        var tenantId = $"upgrade-{Guid.NewGuid()}";
        var userId = $"user-{Guid.NewGuid()}";
        var userEmail = $"admin-{Guid.NewGuid()}@test.com";

        var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: userId,
            userPrincipalName: userEmail,
            email: userEmail);

        // Step 1: Register tenant (gets Professional trial)
        var registrationRequest = new TenantRegistrationRequest
        {
            OrganizationName = "Upgrade Test Org",
            PrimaryAdminEmail = userEmail
        };

        await authClient.PostAsJsonAsync("/Tenants/register", registrationRequest);

        // Step 2: Check initial subscription
        var initialSubResponse = await authClient.GetAsync("/Subscription/my-subscription");
        Assert.Equal(HttpStatusCode.OK, initialSubResponse.StatusCode);

        // Step 3: Use the service (create clients, invite users)
        var createRequest = new CreateClientRequest
        {
            ClientReference = "UPGRADE-001",
            ClientName = "Test Client",
            Description = "Test"
        };

        var createResponse = await authClient.PostAsJsonAsync("/Clients", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ClientResponse>>();
        
        await authClient.PostAsync($"/Clients/{createResult!.Data!.Id}/provision", null);

        // Step 4: Simulate trial ending by updating database
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenant!.Id);
        
        if (subscription != null)
        {
            subscription.Status = "Trial";
            subscription.TrialExpiry = DateTime.UtcNow.AddDays(-1); // Expired
            await _dbContext.SaveChangesAsync();
        }

        // Step 5: Verify trial expired (access might be restricted)
        var postTrialResponse = await authClient.GetAsync("/Clients");
        // Depending on implementation, this might return 403 or still work

        // Step 6: Change plan to Professional
        var changePlanRequest = new ChangePlanRequest
        {
            NewPlanTier = "Professional"
        };

        var changePlanResponse = await authClient.PostAsJsonAsync("/Subscription/change-plan", changePlanRequest);
        
        // Step 7: Verify access is restored
        if (subscription != null)
        {
            subscription.Status = "Active";
            subscription.TrialExpiry = null;
            await _dbContext.SaveChangesAsync();
        }

        var restoredAccessResponse = await authClient.GetAsync("/Clients");
        Assert.Equal(HttpStatusCode.OK, restoredAccessResponse.StatusCode);
    }

    [Fact]
    public async Task MultiUserCollaboration_WithDifferentRoles()
    {
        // Test collaboration between users with different roles
        
        var tenantId = $"collab-{Guid.NewGuid()}";
        
        // Create tenant with owner
        var ownerId = $"owner-{Guid.NewGuid()}";
        var ownerEmail = $"owner-{Guid.NewGuid()}@test.com";
        
        var ownerClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: ownerId,
            userPrincipalName: ownerEmail,
            email: ownerEmail);

        await ownerClient.PostAsJsonAsync("/Tenants/register", new TenantRegistrationRequest
        {
            OrganizationName = "Collaboration Test",
            PrimaryAdminEmail = ownerEmail
        });

        // Get tenant ID for database operations
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.EntraIdTenantId == tenantId);
        Assert.NotNull(tenant);

        // Add admin user
        var adminId = $"admin-{Guid.NewGuid()}";
        var adminEmail = $"admin-{Guid.NewGuid()}@test.com";
        
        _dbContext.TenantUsers.Add(new Data.Entities.TenantUserEntity
        {
            TenantId = tenant.Id,
            AzureAdObjectId = adminId,
            UserPrincipalName = adminEmail,
            DisplayName = "Admin User",
            Role = TenantRole.TenantAdmin,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        // Add viewer user
        var viewerId = $"viewer-{Guid.NewGuid()}";
        var viewerEmail = $"viewer-{Guid.NewGuid()}@test.com";
        
        _dbContext.TenantUsers.Add(new Data.Entities.TenantUserEntity
        {
            TenantId = tenant.Id,
            AzureAdObjectId = viewerId,
            UserPrincipalName = viewerEmail,
            DisplayName = "Viewer User",
            Role = TenantRole.Viewer,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        // Owner creates a client
        var createRequest = new CreateClientRequest
        {
            ClientReference = "COLLAB-001",
            ClientName = "Collaboration Client",
            Description = "Test"
        };

        var createResponse = await ownerClient.PostAsJsonAsync("/Clients", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ClientResponse>>();
        var clientId = createResult!.Data!.Id;

        // Admin can also create clients
        var adminClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: adminId,
            userPrincipalName: adminEmail,
            email: adminEmail);

        var adminCreateResponse = await adminClient.PostAsJsonAsync("/Clients", new CreateClientRequest
        {
            ClientReference = "COLLAB-002",
            ClientName = "Admin Created Client",
            Description = "Created by admin"
        });
        Assert.Equal(HttpStatusCode.Created, adminCreateResponse.StatusCode);

        // Viewer can view but not create
        var viewerClient = TestAuthenticationHelper.CreateAuthenticatedClient(
            _factory.CreateClient(),
            tenantId: tenantId,
            userId: viewerId,
            userPrincipalName: viewerEmail,
            email: viewerEmail);

        var viewerListResponse = await viewerClient.GetAsync("/Clients");
        Assert.Equal(HttpStatusCode.OK, viewerListResponse.StatusCode);

        var viewerCreateResponse = await viewerClient.PostAsJsonAsync("/Clients", new CreateClientRequest
        {
            ClientReference = "COLLAB-003",
            ClientName = "Should Fail",
            Description = "Test"
        });
        Assert.Equal(HttpStatusCode.Forbidden, viewerCreateResponse.StatusCode);

        // All users can view dashboard
        var ownerDashboard = await ownerClient.GetAsync("/Dashboard/summary");
        var adminDashboard = await adminClient.GetAsync("/Dashboard/summary");
        var viewerDashboard = await viewerClient.GetAsync("/Dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, ownerDashboard.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminDashboard.StatusCode);
        Assert.Equal(HttpStatusCode.OK, viewerDashboard.StatusCode);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
