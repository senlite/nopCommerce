using System;
using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Fitment;
using TwinParticles.CheckEngine.Domain.Oem;
using TwinParticles.CheckEngine.Domain.Vehicle;

namespace TwinParticles.CheckEngine.Application.Observability;

public sealed class CheckEngineUninstallExport
{
    public string SchemaVersion { get; init; } = "1";
    public DateTime ExportedUtc { get; init; }
    public IReadOnlyList<VehicleMake> Makes { get; init; } = [];
    public IReadOnlyList<VehicleModel> Models { get; init; } = [];
    public IReadOnlyList<VehicleGeneration> Generations { get; init; } = [];
    public IReadOnlyList<VehicleBody> Bodies { get; init; } = [];
    public IReadOnlyList<VehicleEngine> Engines { get; init; } = [];
    public IReadOnlyList<VehicleMarket> Markets { get; init; } = [];
    public IReadOnlyList<VehicleConfiguration> Configurations { get; init; } = [];
    public IReadOnlyList<VehicleAlias> Aliases { get; init; } = [];
    public IReadOnlyList<Manufacturer> Manufacturers { get; init; } = [];
    public IReadOnlyList<OemNumber> OemNumbers { get; init; } = [];
    public IReadOnlyList<OemRelation> OemRelations { get; init; } = [];
    public IReadOnlyList<FitmentClaim> FitmentClaims { get; init; } = [];
}
