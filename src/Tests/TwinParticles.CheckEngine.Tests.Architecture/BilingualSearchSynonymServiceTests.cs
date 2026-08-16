using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class BilingualSearchSynonymServiceTests
{
    [Test]
    public void Expand_Should_Append_English_Equivalent_For_Arabic_Term()
    {
        var service = new BilingualSearchSynonymService();

        var expanded = service.Expand("فلتر زيت لسيارتي", "ar");

        expanded.Should().Contain("oil filter");
    }

    [Test]
    public void Expand_Should_Append_Arabic_Equivalent_For_Code_Switch_Query()
    {
        var service = new BilingualSearchSynonymService();

        var expanded = service.Expand("brake pad فحمات", "ar");

        expanded.Should().Contain("brake pad");
        expanded.Should().Contain("فحمات فرامل");
    }
}
