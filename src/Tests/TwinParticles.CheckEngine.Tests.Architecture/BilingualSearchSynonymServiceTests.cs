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
}
