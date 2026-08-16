using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// Regression guard for the AI options singleton being hydrated at application start.
///
/// <c>CheckEngineAiOptions.Current</c> is process-wide state that is also written when an administrator
/// opens or saves the configure page. Before this was wired into startup, every application restart (and
/// every extra node in a web farm) served with no enabled AI features, blank provider credentials and a
/// reset disclosure acknowledgement until somebody happened to open that page.
/// </summary>
[TestFixture]
public class CheckEngineStartupAiOptionsTests
{
    [Test]
    public void CheckEngineStartup_Configure_Should_Hydrate_Ai_Options_From_Persisted_Settings()
    {
        var startup = ReadPluginFile("Infrastructure", "CheckEngineStartup.cs");

        startup.Should().Contain("public void Configure(IApplicationBuilder application)");
        startup.Should().Contain("CheckEngineAiSettingsSync.Apply(settings)",
            "AI options must be rehydrated from settings on every application start");
        startup.Should().Contain("LoadSettingAsync<CheckEnginePluginSettings>");
        startup.Should().Contain("DataSettingsManager.IsDatabaseInstalled()",
            "startup hydration must be skipped before the store is installed");
    }

    [Test]
    public void CheckEngineStartup_Configure_Should_Not_Fail_Startup_When_Settings_Cannot_Load()
    {
        var startup = ReadPluginFile("Infrastructure", "CheckEngineStartup.cs");

        startup.Should().Contain("catch (Exception exception)",
            "a settings read failure must not prevent the store from booting");
        startup.Should().Contain("CreateScope()",
            "scoped services such as ISettingService require an explicit scope during startup");
    }

    [Test]
    public void SettingsAiFeatureToggle_Should_Resolve_Through_The_Shared_Options_Singleton()
    {
        var toggle = ReadInfrastructureFile("Ai", "SettingsAiFeatureToggle.cs");

        toggle.Should().Contain("CheckEngineAiOptions.Current",
            "the toggle reads the singleton that startup hydration populates");
    }

    private static string ReadPluginFile(params string[] segments) =>
        ReadRepoFile("Plugins", "TwinParticles.CheckEngine", System.IO.Path.Combine(segments));

    private static string ReadInfrastructureFile(params string[] segments) =>
        ReadRepoFile("Plugins", "TwinParticles.CheckEngine.Infrastructure", System.IO.Path.Combine(segments));

    private static string ReadRepoFile(params string[] segments)
    {
        var path = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            System.IO.Path.Combine(segments));
        path = System.IO.Path.GetFullPath(path);
        System.IO.File.Exists(path).Should().BeTrue(path);
        return System.IO.File.ReadAllText(path);
    }
}
