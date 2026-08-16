using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Application.Ai;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AssistantRetrievalQueryBuilderTests
{
    [Test]
    public void BuildCandidates_Should_Prefer_Known_Part_Phrase_Over_Raw_Question()
    {
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates("Do you have a water pump for my BMW?");

        candidates.First().Should().Be("water pump");
    }

    [Test]
    public void BuildCandidates_Should_Fall_Back_To_Content_Words_For_Unlisted_Parts()
    {
        // "thermostat" is a real catalog part that is not in the known-phrase list; the assistant must
        // still retrieve it rather than searching for the whole sentence.
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates("do you have a thermostat");

        candidates.Should().Contain("thermostat");
        candidates.First().Should().Be("thermostat");
    }

    [Test]
    public void BuildCandidates_Should_Strip_Conversational_Stopwords()
    {
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates("Hi, how much does an alternator cost?");

        candidates.Should().Contain("alternator");
        candidates.Should().NotContain("much");
        candidates.Should().NotContain("does");
    }

    [Test]
    public void BuildCandidates_Should_Keep_Raw_Question_As_Last_Resort()
    {
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates("do you have any");

        candidates.Should().Contain("do you have any");
    }

    [Test]
    public void BuildCandidates_Should_Return_Empty_For_Blank_Input()
    {
        AssistantRetrievalQueryBuilder.BuildCandidates("   ").Should().BeEmpty();
        AssistantRetrievalQueryBuilder.BuildCandidates(null!).Should().BeEmpty();
    }

    [Test]
    public void BuildCandidates_Should_Bound_The_Number_Of_Retrieval_Attempts()
    {
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates(
            "I need a thermostat radiator alternator compressor turbocharger camshaft crankshaft");

        candidates.Should().HaveCountLessThanOrEqualTo(3, "each candidate costs a catalog query");
    }

    [Test]
    public void BuildCandidates_Should_Handle_Arabic_Questions()
    {
        var candidates = AssistantRetrievalQueryBuilder.BuildCandidates("هل لديكم فلتر زيت");

        candidates.First().Should().Be("فلتر زيت");
    }
}
