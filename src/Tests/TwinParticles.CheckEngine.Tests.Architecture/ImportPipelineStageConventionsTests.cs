using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportPipelineStageConventionsTests
{
    [Test]
    public void ImportPipeline_Should_Provide_All_Remaining_Ep09_Stage_Services()
    {
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportDuplicateDetectionService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportOemMatchingService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportVehicleMatchingService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportAiEnrichmentHookService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportTranslationHookService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportSeoGenerationHookService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportCategorizationService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportImageAssignmentService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportReviewService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Stages.ImportPublicationService).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Orchestration.ImportPipelineOrchestratorService).IsClass.Should().BeTrue();
    }
}
