using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.L10n;
using TwinParticles.CheckEngine.Domain.L10n;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AutomotiveGlossaryServiceTests
{
    [Test]
    public void ValidateTranslation_Should_Require_Glossary_Term()
    {
        var service = new AutomotiveGlossaryService();

        service.ValidateTranslation("BMW water pump", "مضخة مياه BMW").Should().BeTrue();
        service.ValidateTranslation("BMW water pump", "مضخة BMW").Should().BeFalse();
    }

    [Test]
    public async Task BuildTranslationPromptAsync_Should_Include_Glossary()
    {
        var service = new AutomotiveGlossaryService();

        var prompt = await service.BuildTranslationPromptAsync("brake pad", "ar", CancellationToken.None);

        prompt.Should().Contain("فحمات فرامل");
    }

    [Test]
    public void EmbeddedGlossary_Should_Load_Extended_Terms()
    {
        var service = new AutomotiveGlossaryService();

        service.ValidateTranslation("upper radiator hose", "خرطوم رديتر علوي").Should().BeTrue();
    }

    [Test]
    public void Overrides_Should_Merge_On_Top_Of_Embedded_Terms()
    {
        var service = new AutomotiveGlossaryService(new FakeOverridesSource(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["brake pad"] = "فحمات مخصصة"
        }));

        service.GetTerms()["brake pad"].Should().Be("فحمات مخصصة");
        service.GetTerms().Should().ContainKey("oil filter");
        service.ValidateTranslation("front brake pad", "فحمات مخصصة").Should().BeTrue();
    }

    private sealed class FakeOverridesSource : IAutomotiveGlossaryOverridesSource
    {
        private readonly IReadOnlyDictionary<string, string> _overrides;

        public FakeOverridesSource(IReadOnlyDictionary<string, string> overrides) => _overrides = overrides;

        public IReadOnlyDictionary<string, string> GetOverrides() => _overrides;
    }
}
