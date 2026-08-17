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

public sealed class SqlVendorRepository : IVendorRepository
{
    private readonly INopDataProvider _dataProvider;

    public SqlVendorRepository(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public async Task<int> InsertAsync(Vendor vendor, CancellationToken cancellationToken)
    {
        var id = await _dataProvider.QueryAsync<int>(@"
INSERT INTO TP_CE_Vendor
(LegalName, TradingName, ContactEmail, ApplicantCustomerId, TaxIdsJson, CategoriesCsv, StatusId, IsOperator, BankingSecretProtected, ApplicantAccessTokenHash, ReviewNotes, CreatedUtc, UpdatedUtc)
VALUES
(@legalName, @tradingName, @contactEmail, @applicantCustomerId, @taxIdsJson, @categoriesCsv, @statusId, @isOperator, @bankingSecretProtected, @applicantAccessTokenHash, @reviewNotes, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId() + @";",
            new DataParameter("legalName", vendor.LegalName),
            new DataParameter("tradingName", vendor.TradingName ?? (object)DBNull.Value),
            new DataParameter("contactEmail", vendor.ContactEmail),
            new DataParameter("applicantCustomerId", vendor.ApplicantCustomerId ?? (object)DBNull.Value),
            new DataParameter("taxIdsJson", vendor.TaxIdsJson),
            new DataParameter("categoriesCsv", vendor.CategoriesCsv),
            new DataParameter("statusId", (int)vendor.Status),
            new DataParameter("isOperator", vendor.IsOperator),
            new DataParameter("bankingSecretProtected", vendor.BankingSecretProtected ?? (object)DBNull.Value),
            new DataParameter("applicantAccessTokenHash", vendor.ApplicantAccessTokenHash ?? (object)DBNull.Value),
            new DataParameter("reviewNotes", vendor.ReviewNotes ?? (object)DBNull.Value),
            new DataParameter("createdUtc", vendor.CreatedUtc.UtcDateTime),
            new DataParameter("updatedUtc", vendor.UpdatedUtc.UtcDateTime));

        return id.FirstOrDefault();
    }

    public Task UpdateAsync(Vendor vendor, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
UPDATE TP_CE_Vendor
SET LegalName = @legalName,
    TradingName = @tradingName,
    ContactEmail = @contactEmail,
    ApplicantCustomerId = @applicantCustomerId,
    TaxIdsJson = @taxIdsJson,
    CategoriesCsv = @categoriesCsv,
    StatusId = @statusId,
    IsOperator = @isOperator,
    ReviewNotes = @reviewNotes,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
            new DataParameter("id", vendor.Id),
            new DataParameter("legalName", vendor.LegalName),
            new DataParameter("tradingName", vendor.TradingName ?? (object)DBNull.Value),
            new DataParameter("contactEmail", vendor.ContactEmail),
            new DataParameter("applicantCustomerId", vendor.ApplicantCustomerId ?? (object)DBNull.Value),
            new DataParameter("taxIdsJson", vendor.TaxIdsJson),
            new DataParameter("categoriesCsv", vendor.CategoriesCsv),
            new DataParameter("statusId", (int)vendor.Status),
            new DataParameter("isOperator", vendor.IsOperator),
            new DataParameter("reviewNotes", vendor.ReviewNotes ?? (object)DBNull.Value),
            new DataParameter("updatedUtc", vendor.UpdatedUtc.UtcDateTime));

    public async Task<Vendor?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<VendorRow>(@"
SELECT Id, LegalName, TradingName, ContactEmail, ApplicantCustomerId, TaxIdsJson, CategoriesCsv, StatusId, IsOperator,
       CASE WHEN BankingSecretProtected IS NULL OR BankingSecretProtected = '' THEN 0 ELSE 1 END AS HasBankingDetails,
       ReviewNotes, CreatedUtc, UpdatedUtc
FROM TP_CE_Vendor
WHERE Id = @id",
            new DataParameter("id", id));

        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Vendor>> GetByStatusesAsync(IReadOnlyCollection<VendorStatus> statuses, CancellationToken cancellationToken)
    {
        if (statuses.Count == 0)
            return Array.Empty<Vendor>();

        var statusList = string.Join(",", statuses.Select(status => ((int)status).ToString()));
        var rows = await _dataProvider.QueryAsync<VendorRow>($@"
SELECT Id, LegalName, TradingName, ContactEmail, ApplicantCustomerId, TaxIdsJson, CategoriesCsv, StatusId, IsOperator,
       CASE WHEN BankingSecretProtected IS NULL OR BankingSecretProtected = '' THEN 0 ELSE 1 END AS HasBankingDetails,
       ReviewNotes, CreatedUtc, UpdatedUtc
FROM TP_CE_Vendor
WHERE StatusId IN ({statusList})
ORDER BY CreatedUtc DESC");

        return rows.Select(Map).ToList();
    }

    public async Task<Vendor?> GetOperatorAsync(CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, LegalName, TradingName, ContactEmail, ApplicantCustomerId, TaxIdsJson, CategoriesCsv, StatusId, IsOperator, CASE WHEN BankingSecretProtected IS NULL OR BankingSecretProtected = '' THEN 0 ELSE 1 END AS HasBankingDetails, ReviewNotes, CreatedUtc, UpdatedUtc",
            "FROM TP_CE_Vendor WHERE IsOperator = 1 ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<VendorRow>(sql);
        return rows.Select(Map).FirstOrDefault();
    }

    public async Task<Vendor?> GetByApplicantCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, LegalName, TradingName, ContactEmail, ApplicantCustomerId, TaxIdsJson, CategoriesCsv, StatusId, IsOperator, CASE WHEN BankingSecretProtected IS NULL OR BankingSecretProtected = '' THEN 0 ELSE 1 END AS HasBankingDetails, ReviewNotes, CreatedUtc, UpdatedUtc",
            "FROM TP_CE_Vendor WHERE ApplicantCustomerId = @customerId ORDER BY Id");
        var rows = await _dataProvider.QueryAsync<VendorRow>(sql, new DataParameter("customerId", customerId));
        return rows.Select(Map).FirstOrDefault();
    }

    public Task InsertAgreementAsync(VendorAgreementAcceptance acceptance, CancellationToken cancellationToken)
        => _dataProvider.ExecuteNonQueryAsync(@"
INSERT INTO TP_CE_VendorAgreementAcceptance (VendorId, AgreementVersion, AcceptedUtc, AcceptedBy)
VALUES (@vendorId, @agreementVersion, @acceptedUtc, @acceptedBy)",
            new DataParameter("vendorId", acceptance.VendorId),
            new DataParameter("agreementVersion", acceptance.AgreementVersion),
            new DataParameter("acceptedUtc", acceptance.AcceptedUtc.UtcDateTime),
            new DataParameter("acceptedBy", acceptance.AcceptedBy));

    public async Task<VendorAgreementAcceptance?> GetLatestAgreementAsync(int vendorId, CancellationToken cancellationToken)
    {
        var sql = CheckEngineSql.SelectTop(1,
            "Id, VendorId, AgreementVersion, AcceptedUtc, AcceptedBy",
            "FROM TP_CE_VendorAgreementAcceptance WHERE VendorId = @vendorId ORDER BY AcceptedUtc DESC");

        var rows = await _dataProvider.QueryAsync<AgreementRow>(sql, new DataParameter("vendorId", vendorId));
        return rows.Select(MapAgreement).FirstOrDefault();
    }

    public async Task<bool> HasAcceptedAgreementAsync(int vendorId, string agreementVersion, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<int>(@"
SELECT COUNT(1) AS Value
FROM TP_CE_VendorAgreementAcceptance
WHERE VendorId = @vendorId AND AgreementVersion = @agreementVersion",
            new DataParameter("vendorId", vendorId),
            new DataParameter("agreementVersion", agreementVersion));

        return rows.FirstOrDefault() > 0;
    }

    public async Task<string?> GetApplicantAccessTokenHashAsync(int vendorId, CancellationToken cancellationToken)
    {
        var rows = await _dataProvider.QueryAsync<string?>(@"
SELECT ApplicantAccessTokenHash
FROM TP_CE_Vendor
WHERE Id = @id",
            new DataParameter("id", vendorId));

        var hash = rows.FirstOrDefault();
        return string.IsNullOrWhiteSpace(hash) ? null : hash;
    }

    private static Vendor Map(VendorRow row)
    {
        return new Vendor
        {
            Id = row.Id,
            LegalName = row.LegalName,
            TradingName = row.TradingName,
            ContactEmail = row.ContactEmail,
            ApplicantCustomerId = row.ApplicantCustomerId,
            TaxIdsJson = row.TaxIdsJson,
            CategoriesCsv = row.CategoriesCsv,
            Status = (VendorStatus)row.StatusId,
            IsOperator = row.IsOperator,
            BankingSecretProtected = null,
            HasBankingDetails = row.HasBankingDetails,
            ReviewNotes = row.ReviewNotes,
            CreatedUtc = ToUtc(row.CreatedUtc),
            UpdatedUtc = ToUtc(row.UpdatedUtc)
        };
    }

    private static VendorAgreementAcceptance MapAgreement(AgreementRow row)
    {
        return new VendorAgreementAcceptance
        {
            Id = row.Id,
            VendorId = row.VendorId,
            AgreementVersion = row.AgreementVersion,
            AcceptedUtc = ToUtc(row.AcceptedUtc),
            AcceptedBy = row.AcceptedBy
        };
    }

    private static DateTimeOffset ToUtc(DateTime value)
        => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class VendorRow
    {
        public int Id { get; set; }
        public string LegalName { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string ContactEmail { get; set; } = string.Empty;
        public int? ApplicantCustomerId { get; set; }
        public string TaxIdsJson { get; set; } = "[]";
        public string CategoriesCsv { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public bool IsOperator { get; set; }
        public bool HasBankingDetails { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }

    private sealed class AgreementRow
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public string AgreementVersion { get; set; } = string.Empty;
        public DateTime AcceptedUtc { get; set; }
        public string AcceptedBy { get; set; } = string.Empty;
    }
}
