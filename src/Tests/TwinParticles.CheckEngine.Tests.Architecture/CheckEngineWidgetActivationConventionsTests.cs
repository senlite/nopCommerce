using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class CheckEngineWidgetActivationConventionsTests
{
    [Test]
    public void Plugin_Should_Activate_Widget_On_Install_And_Update()
    {
        var path = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine", "CheckEnginePlugin.cs");
        path = System.IO.Path.GetFullPath(path);
        var source = System.IO.File.ReadAllText(path);

        source.Should().Contain("EnsureWidgetActiveAsync()");
        source.Should().Contain("ActiveWidgetSystemNames");
        source.Should().Contain("DeactivateWidgetAsync()");
        source.Should().Contain("IWidgetPlugin");
        source.Should().Contain("PublicWidgetZones.HeaderAfter");
        source.Should().Contain("PublicWidgetZones.BodyStartHtmlTagAfter");
        source.Should().Contain("PublicWidgetZones.ProductDetailsTop");
    }
}
