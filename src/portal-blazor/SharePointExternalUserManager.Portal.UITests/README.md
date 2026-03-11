# Blazor Portal UI Tests

This project contains comprehensive UI tests for the ClientSpace Blazor Portal using Playwright.

## What's Tested

The test suite covers **all 12 screens** in the portal application:

### Public Screens (Accessible without login)
1. **Home Page** - Landing page with feature overview
2. **Pricing Page** - Subscription tiers and plan selection
3. **Config Check Page** - Configuration validation page
4. **Error Page** - Error handling page

### Protected Screens (Require authentication)
5. **Dashboard** - Main dashboard for authenticated users
6. **Search** - Global search functionality
7. **Onboarding** - Tenant onboarding wizard
8. **Onboarding Success** - Onboarding completion page
9. **Subscription** - Subscription management
10. **AI Settings** - AI assistant configuration
11. **Tenant Consent** - Azure AD permission consent
12. **Client Detail** - Individual client space details

### Special Tests
- **Login Failure Detection** - Validates that login failures are properly detected through configuration validation
- **Configuration Error Detection** - Verifies that the application's startup validation can detect missing or invalid Azure AD credentials
- **Navigation Menu** - Ensures navigation elements are accessible

## Features

✅ **Screenshot Capture** - All tests automatically capture full-page screenshots
✅ **Login Failure Detection** - Tests verify that login failures due to misconfiguration are caught
✅ **Authentication Testing** - Validates that protected pages require authentication
✅ **Public Page Testing** - Ensures public pages load correctly without authentication
✅ **Comprehensive Coverage** - All 12 portal screens are tested

## Prerequisites

- .NET 8.0 SDK
- Blazor Portal application (must be running on https://localhost:7001)
- Playwright browsers (automatically installed)

## Running the Tests

### 1. Start the Blazor Portal

First, ensure the portal is running:

```bash
cd ../SharePointExternalUserManager.Portal
dotnet run
```

The portal should be accessible at `https://localhost:7001`

### 2. Run All Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "Test01_HomePage_LoadsSuccessfully"
```

### 3. View Screenshots

Screenshots are saved to:
```
SharePointExternalUserManager.Portal.UITests/bin/Debug/net8.0/screenshots/
```

Each screenshot is named with the test name and timestamp:
- `01_HomePage_YYYYMMDD_HHMMSS.png`
- `02_PricingPage_YYYYMMDD_HHMMSS.png`
- etc.

## Test Results

After running tests, you'll see output like:

```
✅ Home page loaded successfully
✅ Pricing page loaded successfully
✅ Config check page loaded successfully
✅ Error page loaded successfully
✅ Dashboard correctly requires authentication
✅ Search page correctly requires authentication
...
```

## Configuration

Tests are configured to:
- Use headless Chromium browser
- Set 30-second navigation timeout
- Ignore HTTPS certificate errors (for local development only)
- Capture full-page screenshots
- Save screenshots with timestamps

## CI/CD Integration

These tests can be integrated into the CI/CD pipeline to:
1. Validate all screens load correctly
2. Ensure authentication is properly enforced
3. Detect login/configuration failures
4. Generate screenshot artifacts for review

## Login Failure Detection

The test suite includes specific tests for login failure scenarios:

1. **Test13_LoginFailure_IsDetected** - Validates the config check page displays Azure AD configuration
2. **Test15_ApplicationStartup_DetectsConfigurationErrors** - Verifies startup validation catches missing credentials

These tests ensure that:
- Missing Azure AD credentials are detected at startup
- Configuration errors are reported clearly
- The config check page provides helpful diagnostic information

## Troubleshooting

### Portal Not Running
If tests fail with connection errors, ensure the portal is running:
```bash
cd ../SharePointExternalUserManager.Portal
dotnet run
```

### Playwright Browsers Not Installed
If you see browser errors, install Playwright browsers:
```bash
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

### HTTPS Certificate Issues
For local development, the tests ignore certificate errors. In production, remove the certificate ignore flag.

## Test Architecture

- **Base Class**: `PageTest` from Playwright.MSTest
- **Browser**: Chromium (headless)
- **Framework**: MSTest
- **Screenshots**: Saved to `screenshots/` directory with timestamps

## Maintenance

When new pages are added to the portal:
1. Add a new test method following the naming pattern `TestNN_PageName_TestDescription`
2. Implement the test following the existing pattern:
   - Navigate to page
   - Wait for page load
   - Assert page loaded correctly
   - Take screenshot
3. Update this README to include the new page

## Future Enhancements

Potential improvements:
- Add authenticated user tests (when auth is configured)
- Test form submissions and interactions
- Add API integration tests
- Test responsive design (mobile/tablet viewports)
- Add accessibility testing
- Performance testing (page load times)

---

**Created**: February 2026
**Framework**: Playwright + MSTest
**Browser**: Chromium
**Target Framework**: .NET 8.0
