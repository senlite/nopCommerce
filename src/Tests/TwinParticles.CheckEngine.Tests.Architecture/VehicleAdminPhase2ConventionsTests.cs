using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class VehicleAdminPhase2ConventionsTests
{
    private static string ReadPluginFile(params string[] relativePath)
    {
        var start = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }

    [Test]
    public void VehicleAdminService_Should_Expose_Crud_For_All_Phase2_Entities()
    {
        var type = typeof(TwinParticles.CheckEngine.Application.Vehicle.Admin.VehicleAdminService);

        var methods = type.GetMethods().Select(x => x.Name).ToList();

        methods.Should().Contain("GetMakesAsync");
        methods.Should().Contain("CreateMakeAsync");
        methods.Should().Contain("UpdateMakeAsync");
        methods.Should().Contain("DeleteMakeAsync");

        methods.Should().Contain("GetModelsAsync");
        methods.Should().Contain("CreateModelAsync");
        methods.Should().Contain("UpdateModelAsync");
        methods.Should().Contain("DeleteModelAsync");

        methods.Should().Contain("GetGenerationsAsync");
        methods.Should().Contain("CreateGenerationAsync");
        methods.Should().Contain("UpdateGenerationAsync");
        methods.Should().Contain("DeleteGenerationAsync");

        methods.Should().Contain("GetBodiesAsync");
        methods.Should().Contain("CreateBodyAsync");
        methods.Should().Contain("UpdateBodyAsync");
        methods.Should().Contain("DeleteBodyAsync");

        methods.Should().Contain("GetEnginesAsync");
        methods.Should().Contain("CreateEngineAsync");
        methods.Should().Contain("UpdateEngineAsync");
        methods.Should().Contain("DeleteEngineAsync");

        methods.Should().Contain("GetMarketsAsync");
        methods.Should().Contain("CreateMarketAsync");
        methods.Should().Contain("UpdateMarketAsync");
        methods.Should().Contain("DeleteMarketAsync");

        methods.Should().Contain("GetConfigurationsAsync");
        methods.Should().Contain("CreateConfigurationAsync");
        methods.Should().Contain("UpdateConfigurationAsync");
        methods.Should().Contain("DeleteConfigurationAsync");

        methods.Should().Contain("GetAliasesAsync");
        methods.Should().Contain("CreateAliasAsync");
        methods.Should().Contain("UpdateAliasAsync");
        methods.Should().Contain("DeleteAliasAsync");

        methods.Should().Contain("SeedAsync");
        methods.Should().Contain("ArchiveMakeAsync");
        methods.Should().Contain("MergeMakeAsync");
        methods.Should().Contain("ArchiveModelAsync");
        methods.Should().Contain("MergeModelAsync");
    }

    [Test]
    public void VehicleAdminLifecycleActions_Should_Bind_Form_Or_Json_Not_FromBody_Only()
    {
        // Browser FormData (multipart) must bind like DeleteModel(int id).
        // [FromBody]-only actions reject multipart with 415 before the action runs.
        var controller = ReadPluginFile("Controllers", "VehicleAdminController.cs");

        controller.Should().Contain("ResolveMergeIdsAsync");
        controller.Should().Contain("TryReadJsonAsync");
        controller.Should().NotContain("[FromBody] VehicleAdminDtos.ArchiveRequestModel");
        controller.Should().NotContain("[FromBody] VehicleAdminDtos.MergeRequestModel");
    }

    [Test]
    public void VehicleSeedLoader_Should_Return_Result_Model()
    {
        var type = typeof(TwinParticles.CheckEngine.Domain.Vehicle.Admin.VehicleSeedLoadResult);

        type.GetProperty("MakesInserted").Should().NotBeNull();
        type.GetProperty("ModelsInserted").Should().NotBeNull();
        type.GetProperty("GenerationsInserted").Should().NotBeNull();
        type.GetProperty("BodiesInserted").Should().NotBeNull();
        type.GetProperty("EnginesInserted").Should().NotBeNull();
        type.GetProperty("MarketsInserted").Should().NotBeNull();
        type.GetProperty("ConfigurationsInserted").Should().NotBeNull();
        type.GetProperty("AliasesInserted").Should().NotBeNull();
    }
}
