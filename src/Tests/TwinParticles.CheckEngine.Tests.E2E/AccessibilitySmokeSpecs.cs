using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class AccessibilitySmokeSpecs
{
    [Test]
    public async Task Home_Should_Expose_Main_Landmark_And_Soft_Check_CheckEngine_Widgets()
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping accessibility smoke.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        await page.GotoAsync(PlaywrightTestSettings.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        var mainCount = await page.Locator("main, [role='main'], #main, .master-wrapper-content").CountAsync();
        mainCount.Should().BeGreaterThan(0, "home page should expose a main landmark / content region");

        var bodyText = await page.Locator("body").InnerTextAsync();
        bodyText.Should().NotContain("An error occurred", "home should not render an obvious error page");

        var themeRoot = page.Locator("[data-ce-theme]");
        if (await themeRoot.CountAsync() == 0)
        {
            Assert.Ignore("CheckEngine theme chrome ([data-ce-theme]) not present — plugin widgets not installed on this host.");
            return;
        }

        var stickySearch = page.Locator("[data-ce-sticky-search], .ce-sticky-search, #ce-search");
        if (await stickySearch.CountAsync() > 0)
        {
            await stickySearch.First.FocusAsync();
            TestContext.Progress.WriteLine("Sticky search widget present and focusable.");
        }
        else
        {
            TestContext.Progress.WriteLine("Sticky search widget absent — soft pass.");
        }

        var garage = page.Locator("[data-ce-garage], .ce-garage-widget, #ce-garage");
        if (await garage.CountAsync() > 0)
            TestContext.Progress.WriteLine("Garage widget present.");
        else
            TestContext.Progress.WriteLine("Garage widget absent — soft pass.");

        var searchInput = page.Locator("input[type='search'], input[name*='q' i], input[placeholder*='search' i], #small-searchterms");
        if (await searchInput.CountAsync() > 0)
        {
            await page.Keyboard.PressAsync("Tab");
            await searchInput.First.FocusAsync();
            (await searchInput.First.EvaluateAsync<bool>("el => document.activeElement === el")).Should().BeTrue();
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

    private static async Task<(IPlaywright Playwright, IBrowser Browser, IPage Page)> LaunchAsync()
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
        var page = await browser.NewPageAsync();
        return (playwright, browser, page);
    }
}
