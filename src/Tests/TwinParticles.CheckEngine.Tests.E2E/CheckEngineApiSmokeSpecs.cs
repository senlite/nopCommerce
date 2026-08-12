using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class CheckEngineApiSmokeSpecs
{
    [Test]
    public async Task Vin_Decode_Endpoint_Should_Respond_Or_Ignore_When_Plugin_Absent()
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping API smoke.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        var response = await page.APIRequest.PostAsync(
            $"{PlaywrightTestSettings.BaseUrl.TrimEnd('/')}/check-engine/vin/decode",
            new APIRequestContextOptions
            {
                DataObject = new { vin = "1HGCM82633A004352" },
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            });

        if (response.Status is 404 or 401 or 403)
        {
            Assert.Ignore($"VIN decode returned {response.Status} — plugin route not installed/authorized on this host.");
            return;
        }

        response.Status.Should().BeOneOf(200, 400, 429);
        var body = await response.TextAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Search_Query_Endpoint_Should_Respond_Or_Ignore_When_Plugin_Absent()
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping API smoke.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        var response = await page.APIRequest.PostAsync(
            $"{PlaywrightTestSettings.BaseUrl.TrimEnd('/')}/check-engine/search/query",
            new APIRequestContextOptions
            {
                DataObject = new { rawText = "oil filter", mode = "Keyword", locale = "en" },
                Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            });

        if (response.Status is 404 or 401 or 403)
        {
            Assert.Ignore($"Search query returned {response.Status} — plugin route not installed/authorized on this host.");
            return;
        }

        response.Status.Should().BeOneOf(200, 400, 429);
        var body = await response.TextAsync();
        body.Should().NotBeNullOrWhiteSpace();
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
