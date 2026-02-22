# SharePoint External User Manager - Integration Tests

This project contains integration and end-to-end (E2E) tests for the SharePoint External User Manager API.

## Overview

Integration tests verify that different components of the application work together correctly, including:
- Database interactions
- Service layer integration
- Controller endpoints
- Microsoft Graph API error handling
- Complete workflow scenarios

## Test Structure

### Test Categories

1. **Workflow Tests** (`Workflows/`)
   - `TenantOnboardingWorkflowTests.cs` - Tests complete tenant signup and consent flow
   - `ClientSiteCreationWorkflowTests.cs` - Tests client site provisioning workflow
   - `ExternalUserManagementWorkflowTests.cs` - Tests external user invite/remove cycles

2. **Graph API Tests** (`GraphApi/`)
   - `GraphErrorHandlingTests.cs` - Tests handling of Graph API errors (401, 403, 404, 429)

### Test Infrastructure

- **TestWebApplicationFactory.cs** - Custom `WebApplicationFactory` that:
  - Configures in-memory database for isolated testing
  - Mocks Microsoft Graph client
  - Mocks SharePoint service for controlled testing
  - Sets up test environment configuration

## Running the Tests

### Run All Integration Tests

```bash
cd src/api-dotnet/WebApi/SharePointExternalUserManager.Api.IntegrationTests
dotnet test
```

### Run Specific Test Category

```bash
# Run only tenant onboarding tests
dotnet test --filter "FullyQualifiedName~TenantOnboardingWorkflowTests"

# Run only Graph error handling tests
dotnet test --filter "FullyQualifiedName~GraphErrorHandlingTests"

# Run only external user management tests
dotnet test --filter "FullyQualifiedName~ExternalUserManagementWorkflowTests"
```

### Run with Detailed Output

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Scenarios Covered

### ✅ Tenant Onboarding Workflow
- [x] Complete tenant signup flow (consent URL → admin consent → tenant creation)
- [x] Consent denied error handling
- [x] Duplicate tenant update handling
- [x] Missing tenant ID validation
- [x] Consent not granted error handling

### ✅ Client Site Creation Workflow
- [x] Complete client site provisioning (create client → provision SharePoint site)
- [x] Provisioning failure handling
- [x] Plan limit enforcement (max clients per tenant)
- [x] Multiple client provisioning

### ✅ External User Management Workflow
- [x] Invite external user to client site
- [x] List external users
- [x] Remove external user from site
- [x] Invalid email error handling
- [x] Non-existent user removal error
- [x] Complete user lifecycle (invite → list → remove)

### ✅ Graph API Error Handling
- [x] 401 Unauthorized (expired/invalid token)
- [x] 403 Forbidden (insufficient permissions)
- [x] 404 Not Found (resource not found)
- [x] 429 Too Many Requests (rate limiting with retry)
- [x] 503 Service Not Available (transient errors with retry)
- [x] Timeout error handling with retry
- [x] Token expiration with retry
- [x] Max retries exceeded error handling

## Key Features

### In-Memory Database
Each test run uses a fresh in-memory database, ensuring:
- Test isolation (no shared state between tests)
- Fast execution (no disk I/O)
- No cleanup required

### Mocked Dependencies
Tests use Moq to mock:
- Microsoft Graph Service Client
- SharePoint Service
- External API calls

This allows testing business logic without requiring:
- Real Azure AD authentication
- Live SharePoint sites
- Active Microsoft 365 subscriptions

### WebApplicationFactory Integration
Tests use `WebApplicationFactory` to:
- Spin up the full application in-memory
- Test HTTP endpoints with real routing
- Verify request/response handling
- Test middleware and filters

## Best Practices

### Test Isolation
Each test is independent and can run in any order. Tests:
- Create their own test data
- Use unique identifiers (GUIDs)
- Don't rely on external state

### Descriptive Test Names
Test names follow the pattern:
```
[FeatureUnderTest]_[Scenario]_[ExpectedResult]
```

Example: `TenantOnboarding_CompleteFlow_Success`

### Arrange-Act-Assert Pattern
Tests follow the AAA pattern:
```csharp
// Arrange - Set up test data and mocks
var tenant = await SetupTestTenant();

// Act - Execute the operation being tested
var result = await service.CreateClientSiteAsync(client, user);

// Assert - Verify expected outcomes
Assert.True(result.Success);
```

## Adding New Tests

To add new integration tests:

1. Create a new test class in the appropriate directory
2. Inherit from `IClassFixture<TestWebApplicationFactory>`
3. Use the factory to access mocked services and database
4. Follow existing test patterns and naming conventions

Example:
```csharp
public class MyNewWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MyNewWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MyFeature_Scenario_ExpectedResult()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Act
        // ...
        
        // Assert
        // ...
    }
}
```

## Continuous Integration

These tests are designed to run in CI/CD pipelines:
- No external dependencies required
- Fast execution (typically < 30 seconds for full suite)
- Clear pass/fail results
- Detailed error messages when tests fail

## Future Enhancements

Potential additions to the test suite:
- [ ] Performance/load testing for Graph API calls
- [ ] More comprehensive error scenario coverage
- [ ] Integration with real Graph API (optional flag)
- [ ] Bulk operation testing
- [ ] Concurrent user management testing
- [ ] RBAC enforcement testing
- [ ] Plan limit enforcement edge cases
