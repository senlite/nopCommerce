using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class InfrastructureScaffoldingConventionsTests
{
    [Test]
    public void Infrastructure_Should_Provide_Required_Scaffolding_Implementations()
    {
        typeof(TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases.MemoryVehicleAliasCache).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases.SqlVehicleAliasRepository).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Observability.LoggerCheckEngineTelemetry).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Infrastructure.Security.DefaultCheckEngineInputSanitizer).IsClass.Should().BeTrue();
        typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import.VehicleAliasImportService).IsClass.Should().BeTrue();
    }
}
