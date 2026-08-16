using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Domain.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiPromptPrivacyTests
{
    [Test]
    public void RedactVins_Should_Keep_Last4_Only()
    {
        var redacted = AiPromptPrivacy.RedactVins("water pump for WBA3A5C53DF350429");

        redacted.Should().Contain("VIN …0429");
        redacted.Should().NotContain("WBA3A5C53DF350429");
        AiPromptPrivacy.ContainsFullVin(redacted).Should().BeFalse();
    }
}
