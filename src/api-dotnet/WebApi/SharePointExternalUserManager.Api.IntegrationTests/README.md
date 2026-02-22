# Integration Tests for SharePoint External User Manager

This directory contains comprehensive integration tests for the SharePoint External User Manager API.

## Overview

The integration tests validate complete workflows and E2E scenarios, including:

- **Tenant Onboarding**: Admin consent, tenant registration, subscription creation
- **Client Site Management**: Creating, provisioning, updating, and deleting client spaces
- **External User Management**: Inviting, removing, and listing external users
- **Error Handling**: Authentication failures, authorization errors, rate limiting
- **E2E Scenarios**: Complete user journeys from onboarding to user management

## Test Structure

```
SharePointExternalUserManager.Api.IntegrationTests/
├── Fixtures/
│   ├── TestWebApplicationFactory.cs    # Custom WebApplicationFactory for testing
│   └── TestAuthenticationHelper.cs     # Test authentication setup and helpers
├── Mocks/
│   └── MockSharePointService.cs        # Mock implementation of ISharePointService
└── Tests/
    ├── TenantOnboardingIntegrationTests.cs
    ├── ClientSiteManagementIntegrationTests.cs
    ├── ExternalUserManagementIntegrationTests.cs
    ├── ErrorHandlingIntegrationTests.cs
    └── EndToEndScenarioTests.cs
```

## Running Tests

### Run all integration tests
```bash
cd src/api-dotnet/WebApi
dotnet test SharePointExternalUserManager.Api.IntegrationTests/
```

### Run specific test class
```bash
dotnet test SharePointExternalUserManager.Api.IntegrationTests/ --filter "FullyQualifiedName~TenantOnboardingIntegrationTests"
```

### Run specific test
```bash
dotnet test SharePointExternalUserManager.Api.IntegrationTests/ --filter "FullyQualifiedName~TenantOnboardingIntegrationTests.GetConsentUrl_ReturnsValidConsentUrl"
```

## Test Configuration

The tests use:
- **In-Memory Database**: Each test gets a fresh in-memory database for isolation
- **Mock Services**: SharePoint/Graph API calls are mocked to avoid real tenant dependencies
- **Test Authentication**: Custom authentication handler that bypasses real Azure AD authentication

## Known Limitations

Some tests may fail due to missing API implementations or different routing:

1. **Authentication Issues**: The test authentication setup might need adjustments for certain endpoints
2. **Rate Limiting**: Rate limiting may interfere with tests - can be disabled via configuration
3. **Missing APIs**: Some tests are commented out due to unimplemented endpoints (bulk operations, update client)
4. **Route Differences**: Some API routes may differ from test expectations

## Fixing Failing Tests

### Authentication Failures (401)

If tests fail with `Unauthorized` responses:
- Verify the `TestAuthenticationHandler` is properly registered
- Check that endpoints have correct `[Authorize]` attributes
- Ensure test clients are created with `TestAuthenticationHelper.CreateAuthenticatedClient()`

### Route Not Found (404)

If tests fail with `NotFound` responses:
- Verify the controller route in the actual API matches the test URL
- Check that the controller method is public and has proper HTTP attribute
- Ensure the controller is properly registered

### Rate Limiting (429)

If tests fail with `TooManyRequests` responses:
- Disable rate limiting in test configuration: `{ "RateLimiting:Enabled", "false" }`
- Or increase rate limits for test environment

## Test Data

Tests create isolated data for each test run:
- Unique tenant IDs (GUID-based)
- Unique user IDs and emails
- Isolated in-memory databases

This ensures tests don't interfere with each other and can run in parallel.

## Extending Tests

When adding new API endpoints, add corresponding integration tests:

1. **Create test method** in appropriate test class
2. **Setup test data** (tenant, users, etc.)
3. **Create authenticated client** using `TestAuthenticationHelper`
4. **Make API calls** and verify responses
5. **Assert database state** if needed

Example:
```csharp
[Fact]
public async Task MyNewEndpoint_WithValidData_ReturnsSuccess()
{
    // Arrange
    var authClient = TestAuthenticationHelper.CreateAuthenticatedClient(
        _factory.CreateClient(),
        tenantId: _tenantId,
        userId: _userId,
        userPrincipalName: _userEmail,
        email: _userEmail);
    
    var request = new MyRequest { /* ... */ };
    
    // Act
    var response = await authClient.PostAsJsonAsync("/api/myendpoint", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // ... more assertions
}
```

## Continuous Integration

These tests should be run as part of CI/CD pipeline:
- Run on every PR
- Run before deployment to staging/production
- Generate code coverage reports

## Troubleshooting

### Tests timing out
- Increase test timeout in xUnit config
- Check for deadlocks or infinite loops in code

### Database errors
- Ensure EF Core InMemory provider is installed
- Check entity configurations and migrations

### Mock service issues
- Verify `MockSharePointService` implements all required interface methods
- Update mocks when interface changes

## Test Coverage

Current coverage includes:
- ✅ Tenant registration and onboarding
- ✅ Role-based access control (RBAC)
- ✅ Client site CRUD operations
- ✅ External user invitation and removal
- ✅ Error handling and validation
- ✅ Complete E2E user journeys
- ⚠️ Bulk operations (partially implemented/commented out)
- ⚠️ AI assistant features (basic coverage)

## Security Testing

The integration tests also serve as security tests:
- Authentication and authorization checks
- RBAC enforcement
- Input validation
- Error message handling (no sensitive data leaked)
