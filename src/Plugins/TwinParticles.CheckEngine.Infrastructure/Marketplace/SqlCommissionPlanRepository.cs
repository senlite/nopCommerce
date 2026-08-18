using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Marketplace;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Marketplace;

public sealed class SqlCommissionPlanRepository : ICommissionPlanRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlCommissionPlanRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<CommissionPlan?> GetActivePlanAsync(int vendorId, CancellationToken cancellationToken)
    {
        var planRows = await _dataProvider.QueryAsync<PlanRow>(CheckEngineSql.SelectTop(
            1,
            "Id, VendorId, Name, IsActive",
            "FROM TP_CE_CommissionPlan WHERE VendorId = @vendorId AND IsActive = 1 ORDER BY Id DESC"),
            new DataParameter("vendorId", vendorId));

        var planRow = planRows.FirstOrDefault();
        if (planRow is null)
            return null;

        var ruleRows = await _dataProvider.QueryAsync<RuleRow>(@"
SELECT Id, PlanId, ModelKindId, BasisId, Priority, FlatAmount, PercentageRate, CategoryId,
       EffectiveFromUtc, EffectiveToUtc, IsActive
FROM TP_CE_CommissionRule
WHERE PlanId = @planId AND IsActive = 1
ORDER BY Priority, Id",
            new DataParameter("planId", planRow.Id));

        var rules = new List<CommissionRule>();
        foreach (var ruleRow in ruleRows)
        {
            var bands = await _dataProvider.QueryAsync<TierRow>(@"
SELECT Id, RuleId, MinVolume, MaxVolume, PercentageRate
FROM TP_CE_CommissionTierBand
WHERE RuleId = @ruleId
ORDER BY MinVolume",
                new DataParameter("ruleId", ruleRow.Id));

            rules.Add(MapRule(ruleRow, bands));
        }

        return new CommissionPlan
        {
            Id = planRow.Id,
            VendorId = planRow.VendorId,
            Name = planRow.Name,
            IsActive = planRow.IsActive,
            Rules = rules
        };
    }

    public async Task UpsertPlanAsync(CommissionPlan plan, CancellationToken cancellationToken)
    {
        if (plan.Id > 0)
        {
            await _dataProvider.ExecuteNonQueryAsync(
                "UPDATE TP_CE_CommissionPlan SET Name = @name, IsActive = @isActive WHERE Id = @id",
                new DataParameter("name", plan.Name),
                new DataParameter("isActive", plan.IsActive),
                new DataParameter("id", plan.Id));
        }
        else
        {
            var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(@"
INSERT INTO TP_CE_CommissionPlan (VendorId, Name, IsActive, CreatedUtc)
VALUES (@vendorId, @name, @isActive, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("vendorId", plan.VendorId),
                new DataParameter("name", plan.Name),
                new DataParameter("isActive", plan.IsActive),
                new DataParameter("createdUtc", DateTime.UtcNow));
            plan.Id = inserted.Single().Value;
        }

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_CommissionTierBand WHERE RuleId IN (SELECT Id FROM TP_CE_CommissionRule WHERE PlanId = @planId)",
            new DataParameter("planId", plan.Id));
        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_CommissionRule WHERE PlanId = @planId",
            new DataParameter("planId", plan.Id));

        foreach (var rule in plan.Rules)
        {
            var insertedRule = await _dataProvider.QueryAsync<ScalarIntRow>(@"
INSERT INTO TP_CE_CommissionRule
(PlanId, ModelKindId, BasisId, Priority, FlatAmount, PercentageRate, CategoryId, EffectiveFromUtc, EffectiveToUtc, IsActive)
VALUES
(@planId, @modelKindId, @basisId, @priority, @flatAmount, @percentageRate, @categoryId, @effectiveFromUtc, @effectiveToUtc, @isActive);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("planId", plan.Id),
                new DataParameter("modelKindId", (int)rule.ModelKind),
                new DataParameter("basisId", (int)rule.Basis),
                new DataParameter("priority", rule.Priority),
                new DataParameter("flatAmount", rule.FlatAmount),
                new DataParameter("percentageRate", rule.PercentageRate),
                new DataParameter("categoryId", rule.CategoryId),
                new DataParameter("effectiveFromUtc", rule.EffectiveFromUtc),
                new DataParameter("effectiveToUtc", rule.EffectiveToUtc),
                new DataParameter("isActive", rule.IsActive));

            var ruleId = insertedRule.Single().Value;
            foreach (var band in rule.TierBands)
            {
                await _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_CommissionTierBand (RuleId, MinVolume, MaxVolume, PercentageRate)
VALUES (@ruleId, @minVolume, @maxVolume, @percentageRate)",
                    new DataParameter("ruleId", ruleId),
                    new DataParameter("minVolume", band.MinVolume),
                    new DataParameter("maxVolume", band.MaxVolume),
                    new DataParameter("percentageRate", band.PercentageRate));
            }
        }
    }

    private static CommissionRule MapRule(RuleRow row, IEnumerable<TierRow> bands)
        => new()
        {
            Id = row.Id,
            PlanId = row.PlanId,
            ModelKind = (CommissionModelKind)row.ModelKindId,
            Basis = (CommissionBasis)row.BasisId,
            Priority = row.Priority,
            FlatAmount = row.FlatAmount,
            PercentageRate = row.PercentageRate,
            CategoryId = row.CategoryId,
            EffectiveFromUtc = row.EffectiveFromUtc,
            EffectiveToUtc = row.EffectiveToUtc,
            IsActive = row.IsActive,
            TierBands = bands.Select(b => new CommissionTierBand
            {
                Id = b.Id,
                RuleId = b.RuleId,
                MinVolume = b.MinVolume,
                MaxVolume = b.MaxVolume,
                PercentageRate = b.PercentageRate
            }).ToList()
        };

    private sealed class PlanRow
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class RuleRow
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int ModelKindId { get; set; }
        public int BasisId { get; set; }
        public int Priority { get; set; }
        public decimal? FlatAmount { get; set; }
        public decimal? PercentageRate { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class TierRow
    {
        public int Id { get; set; }
        public int RuleId { get; set; }
        public decimal MinVolume { get; set; }
        public decimal? MaxVolume { get; set; }
        public decimal PercentageRate { get; set; }
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }
}
