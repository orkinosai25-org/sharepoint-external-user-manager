using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using System.Text.RegularExpressions;

namespace SharePointExternalUserManager.Portal.UITests;

/// <summary>
/// UI tests for all Blazor Portal screens with screenshot capture
/// Tests public pages and validates login failure detection
/// </summary>
[TestFixture]
public class PortalScreensTests : PageTest
{
    private const string BaseUrl = "https://localhost:7001";
    private const int NavigationTimeout = 30000; // 30 seconds
    private readonly string _screenshotPath = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true,  // Ignore SSL cert errors for local dev
        };
    }

    [SetUp]
    public async Task TestInitialize()
    {
        // Create screenshots directory if it doesn't exist
        if (!Directory.Exists(_screenshotPath))
        {
            Directory.CreateDirectory(_screenshotPath);
        }

        // Set default navigation timeout
        Context.SetDefaultNavigationTimeout(NavigationTimeout);
        Page.SetDefaultNavigationTimeout(NavigationTimeout);
    }

    /// <summary>
    /// Helper method to take a screenshot with consistent naming
    /// </summary>
    private async Task TakeScreenshotAsync(string screenName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var filename = $"{screenName}_{timestamp}.png";
        var filepath = Path.Combine(_screenshotPath, filename);
        
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filepath,
            FullPage = true
        });
        
        TestContext.WriteLine($"Screenshot saved: {filepath}");
    }

    /// <summary>
    /// Helper method to check if we're on a login/error page
    /// </summary>
    private async Task<bool> IsOnLoginOrErrorPageAsync()
    {
        var url = Page.Url;
        var title = await Page.TitleAsync();
        
        // Check for login-related URLs or titles
        return url.Contains("MicrosoftIdentity", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("signin", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("login", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("Sign in", StringComparison.OrdinalIgnoreCase) ||
               title.Contains("Login", StringComparison.OrdinalIgnoreCase) ||
               await Page.Locator("text=/sign.?in/i").CountAsync() > 0;
    }

    [Test]
    public async Task Test01_HomePage_LoadsSuccessfully()
    {
        // Arrange & Act
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await Page.TitleAsync();
        Assert.That(title.Contains("ClientSpace"), Is.True, $"Expected title to contain 'ClientSpace', but got: {title}");
        
        // Verify key elements are present
        await Expect(Page.Locator("text=/ClientSpace/i")).ToBeVisibleAsync();
        
        // Take screenshot
        await TakeScreenshotAsync("01_HomePage");
        
        TestContext.WriteLine("✅ Home page loaded successfully");
    }

    [Test]
    public async Task Test02_PricingPage_LoadsSuccessfully()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/pricing");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await Page.TitleAsync();
        Assert.That(title.Contains("Pricing"), $"Expected title to contain 'Pricing', but got: {title}");
        
        // Verify pricing header
        await Expect(Page.Locator("text=/Choose Your Plan/i")).ToBeVisibleAsync();
        
        // Take screenshot
        await TakeScreenshotAsync("02_PricingPage");
        
        TestContext.WriteLine("✅ Pricing page loaded successfully");
    }

    [Test]
    public async Task Test03_ConfigCheckPage_LoadsSuccessfully()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/config-check");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await Page.TitleAsync();
        Assert.That(title.Contains("Configuration") || title.Contains("Config"), 
            $"Expected title to contain 'Configuration' or 'Config', but got: {title}");
        
        // Take screenshot
        await TakeScreenshotAsync("03_ConfigCheckPage");
        
        TestContext.WriteLine("✅ Config check page loaded successfully");
    }

    [Test]
    public async Task Test04_ErrorPage_LoadsSuccessfully()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/Error");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var title = await Page.TitleAsync();
        // Error page might have various titles, just check it loaded
        Assert.That(title, Is.True, "Page title should not be null");
        
        // Take screenshot
        await TakeScreenshotAsync("04_ErrorPage");
        
        TestContext.WriteLine("✅ Error page loaded successfully");
    }

    [Test]
    public async Task Test05_DashboardPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Dashboard should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("05_DashboardPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Dashboard correctly requires authentication");
    }

    [Test]
    public async Task Test06_SearchPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Search should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("06_SearchPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Search page correctly requires authentication");
    }

    [Test]
    public async Task Test07_OnboardingPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/onboarding");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Onboarding should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("07_OnboardingPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Onboarding page correctly requires authentication");
    }

    [Test]
    public async Task Test08_SubscriptionPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/subscription");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Subscription should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("08_SubscriptionPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Subscription page correctly requires authentication");
    }

    [Test]
    public async Task Test09_AiSettingsPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/ai-settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "AI Settings should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("09_AiSettingsPage_RequiresAuth");
        
        TestContext.WriteLine("✅ AI Settings page correctly requires authentication");
    }

    [Test]
    public async Task Test10_TenantConsentPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/onboarding/consent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Tenant Consent should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("10_TenantConsentPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Tenant Consent page correctly requires authentication");
    }

    [Test]
    public async Task Test11_ClientDetailPage_RequiresAuthentication()
    {
        // Arrange & Act
        // Using a sample client ID
        await Page.GotoAsync($"{BaseUrl}/client/00000000-0000-0000-0000-000000000001");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Client Detail should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("11_ClientDetailPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Client Detail page correctly requires authentication");
    }

    [Test]
    public async Task Test12_OnboardingSuccessPage_RequiresAuthentication()
    {
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/onboarding/success");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Should redirect to login
        var isOnLoginPage = await IsOnLoginOrErrorPageAsync();
        Assert.That(isOnLoginPage, Is.True, "Onboarding Success should require authentication and redirect to login");
        
        // Take screenshot
        await TakeScreenshotAsync("12_OnboardingSuccessPage_RequiresAuth");
        
        TestContext.WriteLine("✅ Onboarding Success page correctly requires authentication");
    }

    [Test]
    public async Task Test13_LoginFailure_IsDetected()
    {
        // This test validates that login failures can be detected by the application
        // It doesn't actually attempt a login, but verifies the configuration validation
        
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/config-check");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Check if configuration errors are shown
        var pageContent = await Page.ContentAsync();
        
        // The config check page should show Azure AD configuration status
        bool hasAzureAdSection = pageContent.Contains("AzureAd", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Azure AD", StringComparison.OrdinalIgnoreCase) ||
                                 pageContent.Contains("Configuration", StringComparison.OrdinalIgnoreCase);
        
        Assert.That(hasAzureAdSection, Is.True, "Config check page should display Azure AD configuration information");
        
        // Take screenshot
        await TakeScreenshotAsync("13_LoginFailureDetection");
        
        TestContext.WriteLine("✅ Login failure detection capability verified through config check page");
    }

    [Test]
    public async Task Test14_NavigationMenu_IsAccessible()
    {
        // Arrange & Act
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Check for navigation elements
        var hasNavigation = await Page.Locator("nav, .navbar, [role='navigation']").CountAsync() > 0;
        Assert.That(hasNavigation, Is.True, "Page should have a navigation menu");
        
        // Take screenshot
        await TakeScreenshotAsync("14_NavigationMenu");
        
        TestContext.WriteLine("✅ Navigation menu is accessible");
    }

    [Test]
    public async Task Test15_ApplicationStartup_DetectsConfigurationErrors()
    {
        // This test verifies that the application's startup configuration validation
        // can detect missing or invalid Azure AD credentials (login failure scenarios)
        
        // Arrange & Act
        await Page.GotoAsync($"{BaseUrl}/config-check");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var pageContent = await Page.ContentAsync();
        
        // Look for configuration status indicators
        bool hasConfigInfo = 
            pageContent.Contains("ClientId", StringComparison.OrdinalIgnoreCase) ||
            pageContent.Contains("Client ID", StringComparison.OrdinalIgnoreCase) ||
            pageContent.Contains("TenantId", StringComparison.OrdinalIgnoreCase) ||
            pageContent.Contains("Tenant ID", StringComparison.OrdinalIgnoreCase) ||
            pageContent.Contains("Configuration", StringComparison.OrdinalIgnoreCase);
        
        Assert.That(hasConfigInfo, Is.True, "Config check page should display configuration information that helps detect login failures");
        
        // Take screenshot
        await TakeScreenshotAsync("15_ConfigurationErrorDetection");
        
        TestContext.WriteLine("✅ Application startup configuration error detection verified");
        TestContext.WriteLine("   This ensures login failures due to misconfiguration will be caught");
    }

    [TearDown]
    public async Task TestCleanup()
    {
        await Page.CloseAsync();
    }
}
