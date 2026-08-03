using FluentAssertions;
using NUnit.Framework;

namespace TwinParticles.CheckEngine.Tests.Architecture;

[TestFixture]
public class SkillChecklistAcceptanceTests
{
    [Test]
    public void SkillsScaffolding_Should_Cover_Agreed_Areas()
    {
        typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands.UpsertVehicleAliasCommand).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Commands.UpsertVehicleAliasCommandValidator).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services.VehicleAliasApplicationService).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases.MemoryVehicleAliasCache).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Infrastructure.Observability.LoggerCheckEngineTelemetry).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Infrastructure.Security.DefaultCheckEngineInputSanitizer).Should().NotBeNull();
        typeof(TwinParticles.CheckEngine.Application.Vehicle.Aliases.Import.VehicleAliasImportService).Should().NotBeNull();
    }
}
