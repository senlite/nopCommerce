using System.IO;
using FluentAssertions;
using NUnit.Framework;
using TwinParticles.CheckEngine.Infrastructure.DependencyInjection;
using TwinParticles.CheckEngine.Infrastructure.ImportPipeline;
using TwinParticles.CheckEngine.Infrastructure.Licensing;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class ImportProductionPathContractTests
{
    [Test]
    public void Infrastructure_Di_Should_Register_Sql_Pipeline_And_Nop_Product_Publisher()
    {
        var source = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        source.Should().Contain($"AddScoped<IImportPipelineRepository, {nameof(SqlImportPipelineRepository)}>");
        source.Should().Contain($"AddScoped<IImportProductPublisher, {nameof(NopImportProductPublisher)}>");
    }

    [Test]
    public void Infrastructure_Di_Should_Register_Durable_Licence_State_Store()
    {
        var source = ReadInfrastructureFile("DependencyInjection", "ServiceCollectionExtensions.cs");

        source.Should().Contain(nameof(SqlLicenceStateStore));
        source.Should().Contain("AddScoped<ILicenceStateStore>");
    }

    [Test]
    public void Nop_Publisher_Should_Use_Host_Product_Service_And_Oem_Map_Repository()
    {
        var source = ReadInfrastructureFile("ImportPipeline", "NopImportProductPublisher.cs");

        source.Should().Contain("IProductService");
        source.Should().Contain("IProductOemMapRepository");
        source.Should().Contain("InsertProductAsync");
        source.Should().Contain("UpdateProductAsync");
        source.Should().Contain("_productOemMapRepository.UpsertAsync");
    }

    [Test]
    public void Sql_Pipeline_Repository_Should_Persist_Source_Bytes_And_Row_State()
    {
        var source = ReadInfrastructureFile("ImportPipeline", "SqlImportPipelineRepository.cs");

        source.Should().Contain("SourceContent");
        source.Should().Contain("RunOptionsJson");
        source.Should().Contain("PipelineStateJson");
        source.Should().Contain("ReplaceRowsAsync");
    }

    private static string ReadInfrastructureFile(params string[] relativePath)
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             dir is not null;
             dir = dir.Parent)
        {
            var candidate = Path.Combine([dir.FullName, "src", "Plugins", "TwinParticles.CheckEngine.Infrastructure", .. relativePath]);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Unable to locate {string.Join('/', relativePath)}");
    }
}
