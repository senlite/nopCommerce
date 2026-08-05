namespace TwinParticles.CheckEngine.Tests.E2E;

public static class PlaywrightTestSettings
{
    public static string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://127.0.0.1:5000";

    public static string? BrowserExecutablePath => Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER_PATH");

    public static string? InstallationAdminEmail => Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL");

    public static string? InstallationAdminPassword => Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD");

    public static string? InstallationConnectionString => Environment.GetEnvironmentVariable("E2E_CONNECTION_STRING");

    public static string? InstallationDataProvider => Environment.GetEnvironmentVariable("E2E_DATA_PROVIDER");
}