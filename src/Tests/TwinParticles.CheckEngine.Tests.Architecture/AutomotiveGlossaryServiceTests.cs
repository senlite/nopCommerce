using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.L10n;

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
}
