using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportNormalizationConventionsTests
{
    [Test]
    public void ImportNormalizationService_Should_Expose_NormalizeAsync()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.ImportPipeline.Normalization.ImportNormalizationService);

        type.GetMethod("NormalizeAsync").Should().NotBeNull();
    }

    [Test]
    public void ImportNormalizedRow_Should_Define_Core_Properties()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.ImportPipeline.ImportNormalizedRow);

        type.GetProperty("RowNumber").Should().NotBeNull();
        type.GetProperty("OemNumberRaw").Should().NotBeNull();
        type.GetProperty("OemNumberNormalized").Should().NotBeNull();
        type.GetProperty("Fields").Should().NotBeNull();
    }
}
