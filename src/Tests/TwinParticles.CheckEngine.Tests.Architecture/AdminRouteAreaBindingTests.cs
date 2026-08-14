using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

/// <summary>
/// The admin controllers are decorated with [Area("Admin")]. A conventional route only binds to an
/// area controller when it supplies the area value, otherwise the endpoint 404s. This guards every
/// admin route in the provider against dropping the area value again.
/// </summary>
[TestFixture]
public class AdminRouteAreaBindingTests
{
    private static string ReadRouteProvider()
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", "Infrastructure", "RouteProvider.cs");
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException("Unable to locate RouteProvider.cs");
    }

    [Test]
    public void Every_Admin_Route_Should_Declare_The_Admin_Area()
    {
        var source = ReadRouteProvider();

        // Each MapControllerRoute call is a block starting at "endpointRouteBuilder.MapControllerRoute".
        var blocks = source.Split("endpointRouteBuilder.MapControllerRoute");

        var adminRoutesMissingArea = new List<string>();
        foreach (var block in blocks)
        {
            if (!block.Contains("\"Admin/CheckEngine/"))
                continue;

            if (!block.Contains("area = AreaNames.ADMIN") && !block.Contains("area = \"Admin\""))
            {
                var name = Regex.Match(block, "name: \"(?<n>[^\"]+)\"").Groups["n"].Value;
                adminRoutesMissingArea.Add(string.IsNullOrEmpty(name) ? "<unnamed>" : name);
            }
        }

        adminRoutesMissingArea.Should().BeEmpty(
            "admin routes must bind the Admin area or they 404: {0}", string.Join(", ", adminRoutesMissingArea));
    }

    [Test]
    public void Storefront_Routes_Should_Not_Declare_An_Area()
    {
        var source = ReadRouteProvider();
        var blocks = source.Split("endpointRouteBuilder.MapControllerRoute");

        foreach (var block in blocks)
        {
            if (!block.Contains("\"check-engine/"))
                continue;

            block.Should().NotContain("area = AreaNames.ADMIN",
                "public storefront controllers are not area-scoped");
        }
    }
}
