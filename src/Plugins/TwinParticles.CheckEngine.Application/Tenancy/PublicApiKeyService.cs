using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Tenancy;

namespace TwinParticles.CheckEngine.Application.Tenancy;

public sealed class IssuedApiKey
{
    public required TenantApiKey Record { get; init; }

    /// <summary>Plaintext is returned once at issuance and never stored.</summary>
    public required string Plaintext { get; init; }
}

public sealed class PublicApiAuthentication
{
    public bool Success { get; init; }

    public string? ReasonCode { get; init; }

    public Tenant? Tenant { get; init; }

    public TenantApiKey? ApiKey { get; init; }

    public static PublicApiAuthentication Fail(string reasonCode) => new() { Success = false, ReasonCode = reasonCode };

    public static PublicApiAuthentication Ok(Tenant tenant, TenantApiKey key)
        => new() { Success = true, Tenant = tenant, ApiKey = key };
}

public sealed class PublicApiKeyService
{
    public const string KeyPrefix = "cek_live_";

    private readonly ITenantApiKeyStore _store;
    private readonly ITenantRegistry _registry;
    private readonly TenantRegistryService _registryService;
    private readonly TenantIsolationService _isolation;

    public PublicApiKeyService(
        ITenantApiKeyStore store,
        ITenantRegistry registry,
        TenantRegistryService registryService,
        TenantIsolationService isolation)
    {
        _store = store;
        _registry = registry;
        _registryService = registryService;
        _isolation = isolation;
    }

    public async Task<IssuedApiKey?> IssueAsync(TenantActor actor, int tenantId, string? scopesCsv, CancellationToken cancellationToken)
    {
        var decision = await _isolation.AuthorizeAsync(actor, tenantId, write: true, cancellationToken);
        if (!decision.Allowed)
            return null;

        var tenant = await _registry.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null || tenant.Status != TenantStatus.Active)
            return null;

        var plaintext = KeyPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var record = new TenantApiKey
        {
            TenantId = tenantId,
            KeyPrefix = plaintext[..Math.Min(12, plaintext.Length)],
            KeyHash = Hash(plaintext),
            ScopesCsv = string.IsNullOrWhiteSpace(scopesCsv) ? PublicApiScopes.DefaultCsv : scopesCsv.Trim(),
            IsActive = true,
            CreatedUtc = now
        };
        record.Id = await _store.InsertAsync(record, cancellationToken);
        return new IssuedApiKey { Record = record, Plaintext = plaintext };
    }

    public async Task<PublicApiAuthentication> AuthenticateAsync(string? presentedKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(presentedKey))
            return PublicApiAuthentication.Fail(TenantErrorCodes.PublicApiUnauthenticated);

        var record = await _store.GetByHashAsync(Hash(presentedKey.Trim()), cancellationToken);
        if (record is null || !record.IsActive)
            return PublicApiAuthentication.Fail(TenantErrorCodes.PublicApiKeyInvalid);

        var tenant = await _registry.GetByIdAsync(record.TenantId, cancellationToken);
        if (tenant is null)
            return PublicApiAuthentication.Fail(TenantErrorCodes.TenantNotFound);
        if (tenant.Status == TenantStatus.Suspended)
            return PublicApiAuthentication.Fail(TenantErrorCodes.TenantSuspended);
        if (tenant.Status == TenantStatus.Closed)
            return PublicApiAuthentication.Fail(TenantErrorCodes.TenantClosed);

        _registryService.Bind(tenant);
        await _store.TouchLastUsedAsync(record.Id, DateTimeOffset.UtcNow, cancellationToken);
        return PublicApiAuthentication.Ok(tenant, record);
    }

    public static bool HasScope(TenantApiKey key, string scope)
    {
        if (key is null || string.IsNullOrWhiteSpace(scope))
            return false;
        return key.ScopesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    public static string Hash(string plaintext)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext))).ToLowerInvariant();
}
