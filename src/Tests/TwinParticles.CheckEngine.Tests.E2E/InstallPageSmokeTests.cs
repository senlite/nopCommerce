using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class InstallPageSmokeTests
{
    [Test]
    public async Task Install_Page_Should_Render_And_Save_Artifacts()
    {
        var baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5000";
        var browserExecutablePath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER_PATH");
        var artifactsRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-artifacts", TestContext.CurrentContext.Test.ID));
        Directory.CreateDirectory(artifactsRoot);

        var screenshotPath = Path.Combine(artifactsRoot, "install-page.png");

        using var playwright = await Playwright.CreateAsync();
        var browserLaunchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 100,
            Args = ["--disable-gpu", "--disable-software-rasterizer", "--disable-dev-shm-usage"]
        };

        if (!string.IsNullOrWhiteSpace(browserExecutablePath))
            browserLaunchOptions.ExecutablePath = browserExecutablePath;

        await using var browser = await playwright.Chromium.LaunchAsync(browserLaunchOptions);
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1200 }
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        var installForm = page.Locator("form#installation-form");
        (await installForm.CountAsync()).Should().BeGreaterThan(0);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = screenshotPath,
            FullPage = true
        });

        TestContext.Progress.WriteLine($"Playwright artifacts root: {artifactsRoot}");
        TestContext.Progress.WriteLine($"Playwright screenshot: {screenshotPath}");
    }
}
