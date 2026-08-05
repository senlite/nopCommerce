using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Fitment;

namespace TwinParticles.CheckEngine.Infrastructure.Fitment;

public sealed class SqlFitmentClaimRepository : IFitmentClaimReadRepository, IFitmentClaimWriteRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlFitmentClaimRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<IReadOnlyList<FitmentClaim>> GetClaimsAsync(int productId, int vehicleConfigurationId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<FitmentClaimRow>(@"SELECT c.Id,
       c.ProductId,
       c.VehicleConfigurationId,
       c.OemNumberId,
       c.FitmentStatusId,
       c.Confidence,
       c.SafetyClassId,
       c.SourceKindId,
       c.SourceReference,
       c.CreatedBy,
       c.ProvenanceCreatedUtc,
       c.LastVerifiedUtc,
       c.ValidFromUtc,
       c.ValidToUtc,
       c.IsPublished,
       c.IsActive,
       q.ProductionFromYear,
       q.ProductionToYear,
       q.SteeringSide,
       q.MarketRegion,
       q.DriveType,
       q.TransmissionType,
       q.OptionCodesCsv
FROM TP_CE_FitmentClaim c
LEFT JOIN TP_CE_FitmentQualifier q ON q.FitmentClaimId = c.Id
WHERE c.ProductId = @productId AND c.VehicleConfigurationId = @vehicleConfigurationId", new DataParameter("productId", productId), new DataParameter("vehicleConfigurationId", vehicleConfigurationId));

        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<FitmentClaim>> GetReviewQueueAsync(CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<FitmentClaimRow>(@"SELECT c.Id,
       c.ProductId,
       c.VehicleConfigurationId,
       c.OemNumberId,
       c.FitmentStatusId,
       c.Confidence,
       c.SafetyClassId,
       c.SourceKindId,
       c.SourceReference,
       c.CreatedBy,
       c.ProvenanceCreatedUtc,
       c.LastVerifiedUtc,
       c.ValidFromUtc,
       c.ValidToUtc,
       c.IsPublished,
       c.IsActive,
       q.ProductionFromYear,
       q.ProductionToYear,
       q.SteeringSide,
       q.MarketRegion,
       q.DriveType,
       q.TransmissionType,
       q.OptionCodesCsv
FROM TP_CE_FitmentClaim c
LEFT JOIN TP_CE_FitmentQualifier q ON q.FitmentClaimId = c.Id
WHERE c.IsPublished = 0 OR c.FitmentStatusId IN (3, 4)
ORDER BY c.Confidence DESC");

        return rows.Select(Map).ToList();
    }

    public async Task UpsertAsync(FitmentClaim claim, CancellationToken cancellationToken)
    {
        const string updateClaimSql = @"UPDATE TP_CE_FitmentClaim
SET ProductId = @productId,
    VehicleConfigurationId = @vehicleConfigurationId,
    OemNumberId = @oemNumberId,
    FitmentStatusId = @fitmentStatusId,
    Confidence = @confidence,
    SafetyClassId = @safetyClassId,
    SourceKindId = @sourceKindId,
    SourceReference = @sourceReference,
    CreatedBy = @createdBy,
    ProvenanceCreatedUtc = @provenanceCreatedUtc,
    LastVerifiedUtc = @lastVerifiedUtc,
    ValidFromUtc = @validFromUtc,
    ValidToUtc = @validToUtc,
    IsPublished = @isPublished,
    IsActive = @isActive
WHERE Id = @id";

        var updated = 0;
        if (claim.Id > 0)
        {
            updated = await _dataProvider.ExecuteNonQueryAsync(updateClaimSql,
                new DataParameter("productId", claim.ProductId),
                new DataParameter("vehicleConfigurationId", claim.VehicleConfigurationId),
                new DataParameter("oemNumberId", claim.OemNumberId),
                new DataParameter("fitmentStatusId", (int)claim.Status),
                new DataParameter("confidence", claim.Confidence),
                new DataParameter("safetyClassId", (int)claim.SafetyClass),
                new DataParameter("sourceKindId", (int)claim.Provenance.SourceKind),
                new DataParameter("sourceReference", claim.Provenance.SourceReference),
                new DataParameter("createdBy", claim.Provenance.CreatedBy),
                new DataParameter("provenanceCreatedUtc", claim.Provenance.CreatedUtc.UtcDateTime),
                new DataParameter("lastVerifiedUtc", claim.Provenance.LastVerifiedUtc?.UtcDateTime),
                new DataParameter("validFromUtc", claim.ValidFromUtc),
                new DataParameter("validToUtc", claim.ValidToUtc),
                new DataParameter("isPublished", claim.IsPublished),
                new DataParameter("isActive", claim.IsActive),
                new DataParameter("id", claim.Id));
        }

        if (updated == 0)
        {
            var insertedIdRow = await _dataProvider.QueryAsync<ScalarIntRow>(@"INSERT INTO TP_CE_FitmentClaim
(ProductId, VehicleConfigurationId, OemNumberId, FitmentStatusId, Confidence, SafetyClassId, SourceKindId, SourceReference, CreatedBy, ProvenanceCreatedUtc, LastVerifiedUtc, ValidFromUtc, ValidToUtc, IsPublished, IsActive)
VALUES
(@productId, @vehicleConfigurationId, @oemNumberId, @fitmentStatusId, @confidence, @safetyClassId, @sourceKindId, @sourceReference, @createdBy, @provenanceCreatedUtc, @lastVerifiedUtc, @validFromUtc, @validToUtc, @isPublished, @isActive);
SELECT CAST(SCOPE_IDENTITY() as int) AS Value;",
                new DataParameter("productId", claim.ProductId),
                new DataParameter("vehicleConfigurationId", claim.VehicleConfigurationId),
                new DataParameter("oemNumberId", claim.OemNumberId),
                new DataParameter("fitmentStatusId", (int)claim.Status),
                new DataParameter("confidence", claim.Confidence),
                new DataParameter("safetyClassId", (int)claim.SafetyClass),
                new DataParameter("sourceKindId", (int)claim.Provenance.SourceKind),
                new DataParameter("sourceReference", claim.Provenance.SourceReference),
                new DataParameter("createdBy", claim.Provenance.CreatedBy),
                new DataParameter("provenanceCreatedUtc", claim.Provenance.CreatedUtc.UtcDateTime),
                new DataParameter("lastVerifiedUtc", claim.Provenance.LastVerifiedUtc?.UtcDateTime),
                new DataParameter("validFromUtc", claim.ValidFromUtc),
                new DataParameter("validToUtc", claim.ValidToUtc),
                new DataParameter("isPublished", claim.IsPublished),
                new DataParameter("isActive", claim.IsActive));

            claim.Id = insertedIdRow.Single().Value;
        }

        await _dataProvider.ExecuteNonQueryAsync("DELETE FROM TP_CE_FitmentQualifier WHERE FitmentClaimId = @fitmentClaimId", new DataParameter("fitmentClaimId", claim.Id));

        await _dataProvider.ExecuteNonQueryAsync(@"INSERT INTO TP_CE_FitmentQualifier
(FitmentClaimId, ProductionFromYear, ProductionToYear, SteeringSide, MarketRegion, DriveType, TransmissionType, OptionCodesCsv)
VALUES
(@fitmentClaimId, @productionFromYear, @productionToYear, @steeringSide, @marketRegion, @driveType, @transmissionType, @optionCodesCsv)",
            new DataParameter("fitmentClaimId", claim.Id),
            new DataParameter("productionFromYear", claim.Qualifier.ProductionFromYear),
            new DataParameter("productionToYear", claim.Qualifier.ProductionToYear),
            new DataParameter("steeringSide", claim.Qualifier.SteeringSide),
            new DataParameter("marketRegion", claim.Qualifier.MarketRegion),
            new DataParameter("driveType", claim.Qualifier.DriveType),
            new DataParameter("transmissionType", claim.Qualifier.TransmissionType),
            new DataParameter("optionCodesCsv", claim.Qualifier.OptionCodesCsv));
    }

    public Task SetPublishedAsync(int claimId, bool isPublished, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_FitmentClaim SET IsPublished = @isPublished WHERE Id = @id", new DataParameter("isPublished", isPublished), new DataParameter("id", claimId));

    public Task SetStatusAsync(int claimId, FitmentStatus status, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync("UPDATE TP_CE_FitmentClaim SET FitmentStatusId = @status WHERE Id = @id", new DataParameter("status", (int)status), new DataParameter("id", claimId));

    private static FitmentClaim Map(FitmentClaimRow row)
    {
        return new FitmentClaim
        {
            Id = row.Id,
            ProductId = row.ProductId,
            VehicleConfigurationId = row.VehicleConfigurationId,
            OemNumberId = row.OemNumberId,
            Status = (FitmentStatus)row.FitmentStatusId,
            Confidence = row.Confidence,
            SafetyClass = (SafetyClass)row.SafetyClassId,
            IsPublished = row.IsPublished,
            IsActive = row.IsActive,
            ValidFromUtc = row.ValidFromUtc,
            ValidToUtc = row.ValidToUtc,
            Provenance = new FitmentClaimProvenance
            {
                SourceKind = (FitmentSourceKind)row.SourceKindId,
                SourceReference = row.SourceReference ?? string.Empty,
                CreatedBy = row.CreatedBy ?? string.Empty,
                CreatedUtc = row.ProvenanceCreatedUtc == default ? System.DateTimeOffset.MinValue : new System.DateTimeOffset(row.ProvenanceCreatedUtc),
                LastVerifiedUtc = row.LastVerifiedUtc.HasValue ? new System.DateTimeOffset(row.LastVerifiedUtc.Value) : null
            },
            Qualifier = new FitmentClaimQualifier
            {
                ProductionFromYear = row.ProductionFromYear,
                ProductionToYear = row.ProductionToYear,
                SteeringSide = row.SteeringSide,
                MarketRegion = row.MarketRegion,
                DriveType = row.DriveType,
                TransmissionType = row.TransmissionType,
                OptionCodesCsv = row.OptionCodesCsv
            }
        };
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    private sealed class FitmentClaimRow
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int VehicleConfigurationId { get; set; }
        public int? OemNumberId { get; set; }
        public int FitmentStatusId { get; set; }
        public decimal Confidence { get; set; }
        public int SafetyClassId { get; set; }
        public int SourceKindId { get; set; }
        public string? SourceReference { get; set; }
        public string? CreatedBy { get; set; }
        public System.DateTime ProvenanceCreatedUtc { get; set; }
        public System.DateTime? LastVerifiedUtc { get; set; }
        public System.DateTime? ValidFromUtc { get; set; }
        public System.DateTime? ValidToUtc { get; set; }
        public bool IsPublished { get; set; }
        public bool IsActive { get; set; }
        public int? ProductionFromYear { get; set; }
        public int? ProductionToYear { get; set; }
        public string? SteeringSide { get; set; }
        public string? MarketRegion { get; set; }
        public string? DriveType { get; set; }
        public string? TransmissionType { get; set; }
        public string? OptionCodesCsv { get; set; }
    }
}
