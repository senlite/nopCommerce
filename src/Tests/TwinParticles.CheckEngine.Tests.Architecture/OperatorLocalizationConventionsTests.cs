using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class OperatorLocalizationConventionsTests
{
    [Test]
    public void Licence_Panel_Should_Pass_I18n_Json_To_Admin_Script()
    {
        var view = ReadPluginFile("Views", "Shared", "_LicencePanel.cshtml");
        view.Should().Contain("data-ce-i18n");
        view.Should().Contain("Licence.Panel.Activated");
        view.Should().Contain("Licence.Panel.HeartbeatRecorded");
        view.Should().Contain("Licence.Panel.ActivationFailed");
        view.Should().Contain("Licence.Panel.LoadFailed");
        view.Should().Contain("Licence.Reason.NotActivated");
        view.Should().Contain("Licence.Panel.State.Inactive");
        view.Should().Contain("states");
        view.Should().Contain("reasons");
        view.Should().Contain("data-ce-licence-last-heartbeat");
        view.Should().Contain("data-ce-licence-heartbeat>");
        view.Should().NotContain("<td data-ce-licence-heartbeat");

        var js = ReadPluginFile("Content", "checkengine-licence-admin.js");
        js.Should().Contain("i18n.activated");
        js.Should().Contain("i18n.heartbeatRecorded");
        js.Should().Contain("i18n.loadFailed");
        js.Should().Contain("i18n.yes");
        js.Should().Contain("i18n.states");
        js.Should().Contain("i18n.reasons");
        js.Should().Contain("[data-ce-licence-last-heartbeat]");
        js.Should().Contain("[data-ce-licence-heartbeat]");
        js.Should().NotContain("return 'Yes';");
    }

    [Test]
    public void Configure_Ai_Provider_Options_Should_Use_Locale_Resources()
    {
        var configure = ReadPluginFile("Views", "Configure.cshtml");
        configure.Should().Contain("AiProviderKind.OpenAiCompatible");
        configure.Should().Contain("AiProviderKind.AzureOpenAi");
        configure.Should().Contain("AiProviderKind.Anthropic");
        configure.Should().Contain("AiPerFeatureDailyTokenCeilings.Hint");
        configure.Should().Contain("data-ce-disclosure-i18n");
        configure.Should().Contain("pick(item, 'category', 'Category')");
        configure.Should().Contain("DisclosureLoading");
        configure.Should().Contain("DisclosureLoadFailed");
        configure.Should().NotContain("<option value=\"1\">OpenAI-compatible</option>");
        configure.Should().NotContain("import.ai.enrichment=50000;assistant.customer=20000</small>");
        configure.Should().NotContain("item.category + ':");
        configure.Should().NotContain("Loading disclosure categories…</p>");
    }

    [Test]
    public void Storefront_Chrome_Should_Localize_Unmatched_And_Search_Modes()
    {
        var chrome = ReadPluginFile("Views", "Shared", "Components", "CheckEngineThemeChrome", "Default.cshtml");
        chrome.Should().Contain("\"unmatchedLabel\"");
        chrome.Should().Contain("\"unmatchedHint\"");
        chrome.Should().Contain("Fitment.Unmatched");
        chrome.Should().Contain("\"modeVin\"");
        chrome.Should().Contain("Search.Mode.Vin");
        chrome.Should().Contain("\"searchDegraded\"");

        var js = ReadPluginFile("Content", "checkengine-storefront.js");
        js.Should().Contain("function modeNames()");
        js.Should().Contain("TEXT.searchDegraded");
        js.Should().NotContain("1: 'Auto', 2: 'VIN'");
        js.Should().NotContain("' · degraded'");
    }

    [Test]
    public void Admin_Assets_Should_Localize_Licence_Read_Only_Errors()
    {
        var assets = ReadPluginFile("Views", "Shared", "_CheckEngineAdminAssets.cshtml");
        assets.Should().Contain("Licence.ReadOnly.Message");
        assets.Should().Contain("Common.RequestFailed");
        assets.Should().Contain("CheckEngineAdmin.i18n");

        var marketplace = ReadPluginFile("Views", "Shared", "_MarketplaceAdminAssets.cshtml");
        marketplace.Should().Contain("Licence.ReadOnly.Message");

        ReadPluginFile("Content", "checkengine-admin.js")
            .Should().Contain("CheckEngineAdmin.errorMessage");
        ReadPluginFile("Content", "checkengine-marketplace.js")
            .Should().Contain("CheckEngineAdmin.errorMessage");
    }

    [Test]
    public void Portal_And_Dashboard_Should_Localize_Licence_Read_Only_Errors()
    {
        ReadPluginFile("Views", "Shared", "_PortalI18n.cshtml")
            .Should().Contain("Licence.ReadOnly.Message");
        ReadPluginFile("Views", "Shared", "_PortalAssets.cshtml")
            .Should().Contain("checkengine-portals.js?v=0.95.0");
        ReadPluginFile("Views", "Workshop", "Index.cshtml")
            .Should().Contain("data-ce-error-readonly");
        ReadPluginFile("Views", "Fleet", "Index.cshtml")
            .Should().Contain("data-ce-error-readonly");
        ReadPluginFile("Views", "Dealer", "Index.cshtml")
            .Should().Contain("data-ce-error-readonly");
        ReadPluginFile("Views", "Admin", "PortalAccounts.cshtml")
            .Should().Contain("Licence.ReadOnly.Message");

        var portals = ReadPluginFile("Content", "checkengine-portals.js");
        portals.Should().Contain("i18n.licenceReadOnly");
        portals.Should().Contain("data-ce-error-readonly");
        portals.Should().Contain("function licenceReadOnlyText");
        portals.Should().NotContain("'licence.read_only': 'Check Engine is in licence read-only mode");
        portals.Should().NotContain("return 'Check Engine is in licence read-only mode");

        var dashboard = ReadPluginFile("Views", "Admin", "Dashboard.cshtml");
        dashboard.Should().Contain("CheckEngineAdmin.errorMessage");
        dashboard.Should().Contain("ImportUpload.ChooseFile");
        dashboard.Should().Contain("Import.Batch.Started");
        dashboard.Should().NotContain("showImport('error', 'Choose a file first.')");
        dashboard.Should().NotContain("return 'Check Engine is in licence read-only mode");
    }

    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate TwinParticles.CheckEngine/{string.Join('/', relativePath)}");
    }
}
