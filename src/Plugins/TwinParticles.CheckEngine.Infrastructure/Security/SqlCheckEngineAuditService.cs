using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Core.Domain.Security;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Security;

namespace TwinParticles.CheckEngine.Infrastructure.Security;

public sealed class SqlCheckEngineAuditService : ICheckEngineAuditService, IAuditIntegrityService
{
    private const string ZeroHash = "0000000000000000000000000000000000000000000000000000000000000000";
    private readonly INopDataProvider _dataProvider;
    private readonly string _chainSecret;

    public SqlCheckEngineAuditService(INopDataProvider dataProvider, SecuritySettings securitySettings)
    {
        _dataProvider = dataProvider;
        _chainSecret = securitySettings.EncryptionKey;
    }

    public Task AppendAsync(
        string actor,
        string action,
        string entityType,
        string entityId,
        string? beforeJson,
        string? afterJson,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _dataProvider.ExecuteNonQueryAsync(
            @"SET XACT_ABORT ON;
BEGIN TRANSACTION;
EXEC sp_getapplock @Resource='TP_CE_AuditEvent.HashChain', @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=10000;

DECLARE @previousHash varchar(64) = COALESCE(
    (SELECT TOP (1) EntryHash FROM TP_CE_AuditEvent WHERE EntryHash IS NOT NULL ORDER BY Id DESC),
    (SELECT LastPrunedHash FROM TP_CE_AuditChainAnchor WHERE Id=1),
    @zeroHash);

-- One-time upgrade path: chain legacy rows created before hash columns existed.
DECLARE @legacyId int, @legacyActor nvarchar(256), @legacyAction nvarchar(128),
        @legacyType nvarchar(128), @legacyEntityId nvarchar(128),
        @legacyBefore nvarchar(max), @legacyAfter nvarchar(max), @legacyCreated datetime2;
DECLARE legacy CURSOR LOCAL FAST_FORWARD FOR
    SELECT Id, Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc
    FROM TP_CE_AuditEvent WHERE EntryHash IS NULL ORDER BY Id;
OPEN legacy;
FETCH NEXT FROM legacy INTO @legacyId, @legacyActor, @legacyAction, @legacyType,
    @legacyEntityId, @legacyBefore, @legacyAfter, @legacyCreated;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @legacyHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256',
        CONCAT(@secret, NCHAR(31), @previousHash, NCHAR(31), @legacyActor, NCHAR(31),
        @legacyAction, NCHAR(31), @legacyType, NCHAR(31), @legacyEntityId, NCHAR(31),
        COALESCE(@legacyBefore,N''), NCHAR(31), COALESCE(@legacyAfter,N''), NCHAR(31),
        CONVERT(nvarchar(33),@legacyCreated,126))), 2);
    UPDATE TP_CE_AuditEvent SET PreviousHash=@previousHash, EntryHash=@legacyHash WHERE Id=@legacyId;
    SET @previousHash=@legacyHash;
    FETCH NEXT FROM legacy INTO @legacyId, @legacyActor, @legacyAction, @legacyType,
        @legacyEntityId, @legacyBefore, @legacyAfter, @legacyCreated;
END;
CLOSE legacy;
DEALLOCATE legacy;

DECLARE @entryHash varchar(64) = CONVERT(varchar(64), HASHBYTES('SHA2_256',
    CONCAT(@secret, NCHAR(31), @previousHash, NCHAR(31), @actor, NCHAR(31), @action,
    NCHAR(31), @entityType, NCHAR(31), @entityId, NCHAR(31), COALESCE(@beforeJson,N''),
    NCHAR(31), COALESCE(@afterJson,N''), NCHAR(31), CONVERT(nvarchar(33),@createdUtc,126))), 2);

INSERT INTO TP_CE_AuditEvent
    (Actor, Action, EntityType, EntityId, BeforeJson, AfterJson, CreatedUtc, PreviousHash, EntryHash)
VALUES
    (@actor, @action, @entityType, @entityId, @beforeJson, @afterJson, @createdUtc, @previousHash, @entryHash);
COMMIT TRANSACTION;",
            new DataParameter("actor", actor ?? string.Empty),
            new DataParameter("action", action ?? string.Empty),
            new DataParameter("entityType", entityType ?? string.Empty),
            new DataParameter("entityId", entityId ?? string.Empty),
            new DataParameter("beforeJson", (object?)beforeJson ?? DBNull.Value),
            new DataParameter("afterJson", (object?)afterJson ?? DBNull.Value),
            new DataParameter("createdUtc", DateTime.UtcNow),
            new DataParameter("secret", _chainSecret),
            new DataParameter("zeroHash", ZeroHash));
    }

    public async Task<AuditIntegrityResult> VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _dataProvider.QueryAsync<IntegrityRow>(
            @"DECLARE @anchor varchar(64)=COALESCE(
    (SELECT LastPrunedHash FROM TP_CE_AuditChainAnchor WHERE Id=1), @zeroHash);
;WITH chain AS
(
    SELECT *, LAG(EntryHash,1,@anchor) OVER (ORDER BY Id) AS ExpectedPreviousHash
    FROM TP_CE_AuditEvent
)
SELECT COUNT(*) AS CheckedEntries,
       COALESCE(SUM(CASE WHEN EntryHash IS NULL OR PreviousHash<>ExpectedPreviousHash
         OR EntryHash<>CONVERT(varchar(64),HASHBYTES('SHA2_256',
            CONCAT(@secret,NCHAR(31),PreviousHash,NCHAR(31),Actor,NCHAR(31),Action,
            NCHAR(31),EntityType,NCHAR(31),EntityId,NCHAR(31),COALESCE(BeforeJson,N''),
            NCHAR(31),COALESCE(AfterJson,N''),NCHAR(31),CONVERT(nvarchar(33),CreatedUtc,126))),2)
         THEN 1 ELSE 0 END),0) AS InvalidEntries
FROM chain;",
            new DataParameter("secret", _chainSecret),
            new DataParameter("zeroHash", ZeroHash));
        var row = rows[0];
        return new AuditIntegrityResult
        {
            IsValid = row.InvalidEntries == 0,
            CheckedEntries = row.CheckedEntries,
            InvalidEntries = row.InvalidEntries
        };
    }

    public async Task<int> PruneAsync(DateTime retainFromUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _dataProvider.QueryAsync<PruneRow>(
            @"SET XACT_ABORT ON;
BEGIN TRANSACTION;
EXEC sp_getapplock @Resource='TP_CE_AuditEvent.HashChain', @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=10000;
DECLARE @pruneId int, @pruneHash varchar(64);
SELECT TOP (1) @pruneId=Id, @pruneHash=EntryHash
FROM TP_CE_AuditEvent
WHERE CreatedUtc<@retainFromUtc AND EntryHash IS NOT NULL
ORDER BY Id DESC;
IF @pruneId IS NULL
BEGIN
    COMMIT TRANSACTION;
    SELECT 0 AS Deleted;
    RETURN;
END;
MERGE TP_CE_AuditChainAnchor WITH (HOLDLOCK) AS target
USING (SELECT 1 AS Id) AS source ON target.Id=source.Id
WHEN MATCHED THEN UPDATE SET LastPrunedAuditEventId=@pruneId,
    LastPrunedHash=@pruneHash, PrunedUtc=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT (Id,LastPrunedAuditEventId,LastPrunedHash,PrunedUtc)
    VALUES (1,@pruneId,@pruneHash,SYSUTCDATETIME());
DELETE FROM TP_CE_AuditEvent WHERE Id<=@pruneId;
DECLARE @deleted int=@@ROWCOUNT;
COMMIT TRANSACTION;
SELECT @deleted AS Deleted;",
            new DataParameter("retainFromUtc", retainFromUtc));
        return rows[0].Deleted;
    }

    private sealed class IntegrityRow
    {
        public int CheckedEntries { get; set; }
        public int InvalidEntries { get; set; }
    }

    private sealed class PruneRow
    {
        public int Deleted { get; set; }
    }
}
