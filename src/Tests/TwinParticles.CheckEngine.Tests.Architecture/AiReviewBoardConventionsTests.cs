using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class AiReviewBoardConventionsTests
{
    [Test]
    public void Review_Board_Should_Disable_Approve_When_Quality_Score_Blocks()
    {
        var view = ReadPluginFile("Views", "Admin", "AiReview.cshtml");

        view.Should().Contain("function approveBlocked(");
        view.Should().Contain("function approveBlockReason(");
        view.Should().Contain("qualityScore < 1");
        view.Should().Contain("Translation");
        view.Should().Contain("Specification");
        view.Should().Contain("CheckEngineAdmin.errorMessage");
        view.Should().Contain("Review blocked.");
        view.Should().Contain("/Admin/CheckEngine/AiAdmin/Review");
    }

    [Test]
    public void Fitment_Review_Board_Should_Show_Rationale_And_Actions()
    {
        var view = ReadPluginFile("Views", "Admin", "FitmentAiReview.cshtml");

        view.Should().Contain("sourceReference");
        view.Should().Contain("data-action=\"approve\"");
        view.Should().Contain("data-action=\"reject\"");
        view.Should().Contain("/Admin/CheckEngine/FitmentAdmin/Approve");
        view.Should().Contain("/Admin/CheckEngine/FitmentAdmin/Reject");
        view.Should().Contain("/Admin/CheckEngine/FitmentAdmin/Infer");
    }

    private static string ReadPluginFile(params string[] segments)
    {
        var path = System.IO.Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "Plugins", "TwinParticles.CheckEngine",
            System.IO.Path.Combine(segments));
        path = System.IO.Path.GetFullPath(path);
        System.IO.File.Exists(path).Should().BeTrue(path);
        return System.IO.File.ReadAllText(path);
    }
}
