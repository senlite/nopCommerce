using FluentAssertions;
using Microsoft.Playwright;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.E2E;

[TestFixture]
[NonParallelizable]
public class InstallAndSmokeSpecs
{
    private const string DefaultAdminEmail = "admin@yourStore.com";
    private const string DefaultAdminPassword = "Admin123$";
    private const string DefaultConnectionString = "Host=db;Port=5432;Database=nopCommerce;Username=nopCommerce;Password=nopCommerce_db_password123!";
    private const string SampleProductSlug = "lenovo-thinkpad-x1-carbon-laptop";

    [Test, Order(1)]
    public async Task Install_And_Seed_Should_Complete()
    {
        var artifactsRoot = CreateArtifactsRoot();
        var installScreenshotPath = Path.Combine(artifactsRoot, "install-page.png");
        var homeScreenshotPath = Path.Combine(artifactsRoot, "home-after-install.png");
        var tracePath = Path.Combine(artifactsRoot, "install-flow-trace.zip");

        var (playwright, browser) = await LaunchBrowserAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1200 },
            RecordVideoDir = artifactsRoot,
            RecordVideoSize = new RecordVideoSize { Width = 1440, Height = 1200 }
        });
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();

        await WaitForServerAsync(page, PlaywrightTestSettings.BaseUrl);

        (await page.Locator("form#installation-form").CountAsync()).Should().BeGreaterThan(0);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = installScreenshotPath,
            FullPage = true
        });

        await page.Locator("input[name='AdminEmail']").FillAsync(PlaywrightTestSettings.InstallationAdminEmail ?? DefaultAdminEmail);
        await page.Locator("input[name='AdminPassword']").FillAsync(PlaywrightTestSettings.InstallationAdminPassword ?? DefaultAdminPassword);
        await page.Locator("input[name='ConfirmPassword']").FillAsync(PlaywrightTestSettings.InstallationAdminPassword ?? DefaultAdminPassword);
        await page.Locator("select[name='DataProvider']").SelectOptionAsync(PlaywrightTestSettings.InstallationDataProvider ?? "3");
        await page.Locator("#ConnectionStringRaw").CheckAsync();
        await page.Locator("input[name='ConnectionString']").FillAsync(PlaywrightTestSettings.InstallationConnectionString ?? DefaultConnectionString);
        await page.Locator("#CreateDatabaseIfNotExists").CheckAsync();
        if (!await page.Locator("#InstallSampleData").IsCheckedAsync())
            await page.Locator("#InstallSampleData").CheckAsync();

        await page.GetByRole(AriaRole.Button, new() { Name = "Install", Exact = true }).ClickAsync();
        await WaitForHomePageAsync(page, PlaywrightTestSettings.BaseUrl, TimeSpan.FromMinutes(5));

        (await page.Locator("div.home-page").CountAsync()).Should().BeGreaterThan(0);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = homeScreenshotPath,
            FullPage = true
        });

        await context.Tracing.StopAsync(new TracingStopOptions
        {
            Path = tracePath
        });

        TestContext.Progress.WriteLine($"Playwright artifacts root: {artifactsRoot}");
        TestContext.Progress.WriteLine($"Install screenshot: {installScreenshotPath}");
        TestContext.Progress.WriteLine($"Home screenshot: {homeScreenshotPath}");
        TestContext.Progress.WriteLine($"Trace archive: {tracePath}");
    }

    [Test, Order(2)]
    public async Task Home_Page_Should_Render()
    {
        var (playwright, browser) = await LaunchBrowserAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1200 }
        });
        var page = await context.NewPageAsync();

        await WaitForServerAsync(page, PlaywrightTestSettings.BaseUrl);

        (await page.Locator("div.home-page").CountAsync()).Should().BeGreaterThan(0);
    }

    [Test, Order(3)]
    public async Task Search_Page_Should_Render_And_Return_Results()
    {
        var (playwright, browser) = await LaunchBrowserAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1200 }
        });
        var page = await context.NewPageAsync();

        await WaitForServerAsync(page, $"{PlaywrightTestSettings.BaseUrl}/search?q=Lenovo");

        (await page.Locator("div.search-page").CountAsync()).Should().BeGreaterThan(0);
        (await page.Locator("input.search-text").CountAsync()).Should().BeGreaterThan(0);
        (await page.Locator(".product-item").CountAsync()).Should().BeGreaterThan(0);
        (await page.Locator("body").InnerTextAsync()).Should().Contain("Lenovo");
    }

    [Test, Order(4)]
    public async Task Product_Detail_Page_Should_Render_For_Known_Sample_Data()
    {
        var (playwright, browser) = await LaunchBrowserAsync();
        using var playwrightLifetime = playwright;
        await using var browserLifetime = browser;
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 1200 }
        });
        var page = await context.NewPageAsync();

        await WaitForServerAsync(page, $"{PlaywrightTestSettings.BaseUrl}/{SampleProductSlug}");

        (await page.Locator("main, .product-details-page, .product-page").CountAsync()).Should().BeGreaterThan(0);
        (await page.Locator("body").InnerTextAsync()).Should().Contain("ThinkPad");
    }

    private static string CreateArtifactsRoot()
    {
        var artifactsRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-artifacts", TestContext.CurrentContext.Test.ID));
        Directory.CreateDirectory(artifactsRoot);
        return artifactsRoot;
    }

    private static async Task WaitForServerAsync(IPage page, string url)
    {
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var response = await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                if (response?.Ok == true)
                    return;
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        throw new TimeoutException($"The target site at '{url}' did not become ready in time.");
    }

    private static async Task WaitForHomePageAsync(IPage page, string baseUrl, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await page.GotoAsync(baseUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });

                if (await page.Locator("div.home-page").CountAsync() > 0)
                    return;
            }
            catch
            {
                // ignore and retry until the app restarts and the home page becomes available
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        throw new TimeoutException($"The home page at '{baseUrl}' did not become available after installation within the allotted time.");
    }

    private static async Task<(IPlaywright Playwright, IBrowser Browser)> LaunchBrowserAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browserLaunchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true,
            SlowMo = 100,
            Args = ["--disable-gpu", "--disable-software-rasterizer", "--disable-dev-shm-usage"]
        };

        if (!string.IsNullOrWhiteSpace(PlaywrightTestSettings.BrowserExecutablePath))
            browserLaunchOptions.ExecutablePath = PlaywrightTestSettings.BrowserExecutablePath;

        var browser = await playwright.Chromium.LaunchAsync(browserLaunchOptions);
        return (playwright, browser);
    }
}
