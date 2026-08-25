using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class AccessibilityAxeSpecs
{
    public static IEnumerable<string> KeyTemplates =>
    [
        "/",
        "/search?q=filter",
        "/computers",
        "/en/gmaster-bmw-parts-5",
        "/build-your-own-computer",
        "/en/gmaster-gm-11127548196-2"
    ];

    [TestCaseSource(nameof(KeyTemplates))]
    public async Task CheckEngine_Surfaces_Should_Have_No_Serious_Or_Critical_Axe_Findings(string path)
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping axe gate.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        var response = await page.GotoAsync(
            PlaywrightTestSettings.BaseUrl.TrimEnd('/') + path,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30_000 });

        if (response is { Ok: false } && response.Status == 404)
        {
            Assert.Ignore($"Template {path} is not installed on this host.");
            return;
        }

        var themeCount = await page.Locator(".ce-root, [data-ce-theme]").CountAsync();
        if (themeCount == 0)
        {
            Assert.Ignore("Check Engine surfaces are not present — plugin widgets not installed on this host.");
            return;
        }

        await InjectAxeAsync(page);
        var findings = await ScanCheckEngineSurfacesAsync(page);

        TestContext.Progress.WriteLine(
            $"axe {path}: {findings.Length} serious/critical Check Engine finding(s).");
        foreach (var finding in findings)
            TestContext.Progress.WriteLine($"  [{finding.Impact}] {finding.Id}: {finding.Description} ({finding.Nodes} nodes)");

        findings.Should().BeEmpty(
            $"NFR-046 requires zero serious/critical axe findings on Check Engine surfaces for {path}");
    }

    [Test]
    public async Task Arabic_Storefront_CheckEngine_Surfaces_Should_Have_No_Serious_Or_Critical_Axe_Findings()
    {
        if (!await IsServerReachableAsync())
        {
            Assert.Ignore("E2E server not reachable — skipping Arabic axe gate.");
            return;
        }

        var (playwright, browser, page) = await LaunchAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;

        var arUrl = PlaywrightTestSettings.BaseUrl.TrimEnd('/') + "/ar/";
        var response = await page.GotoAsync(arUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30_000
        });

        if (response is { Ok: false })
        {
            Assert.Ignore($"Arabic storefront is not published on this host (HTTP {response.Status}).");
            return;
        }

        var dir = await page.EvaluateAsync<string?>(
            "document.documentElement.getAttribute('dir') || document.body?.getAttribute('dir') || getComputedStyle(document.documentElement).direction");
        if (!string.Equals(dir, "rtl", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Arabic storefront did not render RTL — language SEO URLs may be disabled.");
            return;
        }

        var themeCount = await page.Locator(".ce-root, [data-ce-theme]").CountAsync();
        if (themeCount == 0)
        {
            Assert.Ignore("Check Engine surfaces are not present on /ar/.");
            return;
        }

        await InjectAxeAsync(page);
        var findings = await ScanCheckEngineSurfacesAsync(page);
        findings.Should().BeEmpty("NFR-046 applies to the Arabic RTL Check Engine chrome");
    }

    private static async Task InjectAxeAsync(IPage page)
    {
        var axePath = await EnsureAxeCoreAsync();
        await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = axePath });
    }

    private static async Task<AxeFinding[]> ScanCheckEngineSurfacesAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            async () => {
              const results = await axe.run(
                { include: [['.ce-root'], ['[data-ce-theme]']] },
                { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] } });
              const findings = (results.violations || [])
                .filter(v => v.impact === 'serious' || v.impact === 'critical')
                .map(v => ({
                  id: v.id,
                  impact: v.impact,
                  description: v.description,
                  nodes: (v.nodes || []).length
                }));
              return JSON.stringify(findings);
            }
            """);
        return JsonSerializer.Deserialize<AxeFinding[]>(json ?? "[]") ?? [];
    }

    private static async Task<string> EnsureAxeCoreAsync()
    {
        var configured = Environment.GetEnvironmentVariable("AXE_CORE_PATH");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        var cache = Path.Combine(Path.GetTempPath(), "checkengine-axe", "axe.min.js");
        if (File.Exists(cache) && new FileInfo(cache).Length > 1000)
            return cache;

        Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var bytes = await client.GetByteArrayAsync(
            "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.3/axe.min.js");
        await File.WriteAllBytesAsync(cache, bytes);
        return cache;
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
            Args = ["--disable-gpu", "--disable-dev-shm-usage", "--no-sandbox"]
        };

        if (!string.IsNullOrWhiteSpace(PlaywrightTestSettings.BrowserExecutablePath))
            options.ExecutablePath = PlaywrightTestSettings.BrowserExecutablePath;

        var browser = await playwright.Chromium.LaunchAsync(options);
        var page = await browser.NewPageAsync();
        return (playwright, browser, page);
    }

    private sealed record AxeFinding(string Id, string Impact, string Description, int Nodes);
}
