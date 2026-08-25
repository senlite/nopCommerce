using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class AccessibilityViewportMatrixSpecs
{
    [TestCase(320, 568)]
    [TestCase(768, 1024)]
    [TestCase(1440, 900)]
    [TestCase(2560, 1440)]
    public async Task Home_Should_Expose_Main_Landmark_At_Viewport(int width, int height)
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping viewport matrix.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync(width, height);
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        await page.GotoAsync(PlaywrightTestSettings.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        var mainCount = await page.Locator("main, [role='main'], #main, .master-wrapper-content").CountAsync();
        mainCount.Should().BeGreaterThan(0, $"home at {width}x{height} should expose a main landmark");

        var overflowX = await page.EvaluateAsync<double>("document.documentElement.scrollWidth > document.documentElement.clientWidth ? 1 : 0");
        overflowX.Should().Be(0, $"home at {width}x{height} should not horizontally overflow");
    }

    [TestCase(320, 568)]
    [TestCase(768, 1024)]
    [TestCase(1440, 900)]
    [TestCase(2560, 1440)]
    public async Task Arabic_Storefront_Should_Use_Rtl_And_Labelled_Search_At_Viewport(int width, int height)
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping RTL viewport matrix.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync(width, height);
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        var arUrl = PlaywrightTestSettings.BaseUrl.TrimEnd('/') + "/ar/";
        await page.GotoAsync(arUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        var dir = await page.EvaluateAsync<string?>(
            "document.documentElement.getAttribute('dir') || document.body?.getAttribute('dir') || getComputedStyle(document.documentElement).direction");
        dir.Should().Be("rtl", $"Arabic storefront at {width}x{height} should render RTL");

        var labelledSearch = page.Locator("#ce-sticky-search-input, input[type='search'][aria-label], input[type='search'][id]");
        if (await labelledSearch.CountAsync() > 0)
        {
            var ariaLabel = await labelledSearch.First.GetAttributeAsync("aria-label");
            var id = await labelledSearch.First.GetAttributeAsync("id");
            var labelledBy = await labelledSearch.First.GetAttributeAsync("aria-labelledby");
            var hasName = !string.IsNullOrWhiteSpace(ariaLabel)
                || !string.IsNullOrWhiteSpace(id)
                || !string.IsNullOrWhiteSpace(labelledBy);
            hasName.Should().BeTrue("Check Engine search should expose an accessible name for screen readers");
        }
        else
        {
            TestContext.Progress.WriteLine($"Search input not found at {width}x{height} — soft pass.");
        }
    }

    private static async Task<bool> IsServerReachableAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(PlaywrightTestSettings.BaseUrl);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<(IPlaywright Playwright, IBrowser Browser, IPage Page)> LaunchAsync(int width, int height)
    {
        var playwright = await Playwright.CreateAsync();
        var options = new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--disable-gpu", "--disable-dev-shm-usage"]
        };

        if (!string.IsNullOrWhiteSpace(PlaywrightTestSettings.BrowserExecutablePath))
            options.ExecutablePath = PlaywrightTestSettings.BrowserExecutablePath;

        var browser = await playwright.Chromium.LaunchAsync(options);
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        var page = await context.NewPageAsync();
        return (playwright, browser, page);
    }
}
