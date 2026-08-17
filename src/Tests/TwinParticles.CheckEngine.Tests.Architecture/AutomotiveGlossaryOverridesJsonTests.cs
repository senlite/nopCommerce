using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.L10n;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AutomotiveGlossaryOverridesJsonTests
{
    [Test]
    public void Parse_Should_Return_Empty_For_Null_Or_Invalid_Json()
    {
        AutomotiveGlossaryOverridesJson.Parse(null).Should().BeEmpty();
        AutomotiveGlossaryOverridesJson.Parse("not-json").Should().BeEmpty();
        AutomotiveGlossaryOverridesJson.Parse("{}").Should().BeEmpty();
    }

    [Test]
    public void Serialize_Should_Round_Trip_Case_Insensitive_Keys()
    {
        var input = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Water Pump"] = "مضخة مياه",
            ["brake pad"] = "فحمات فرامل"
        };

        var json = AutomotiveGlossaryOverridesJson.Serialize(input);
        var parsed = AutomotiveGlossaryOverridesJson.Parse(json);

        parsed.Should().HaveCount(2);
        parsed["water pump"].Should().Be("مضخة مياه");
        parsed["Brake Pad"].Should().Be("فحمات فرامل");
    }
}
