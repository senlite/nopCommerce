using System;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Domain.Licensing;
using TwinParticles.CheckEngine.Domain.Performance;

namespace TwinParticles.CheckEngine.Application.Licensing;

public sealed class DefaultLicenceService : ILicenceService
{
    private readonly ICheckEngineClock _clock;
    private readonly ILicenceStateStore _stateStore;
    private readonly ILicenceKeyValidator _licenceKeyValidator;

    public DefaultLicenceService(
        ILicenceStateStore stateStore,
        ICheckEngineClock clock,
        ILicenceKeyValidator licenceKeyValidator)
    {
        _stateStore = stateStore;
        _clock = clock;
        _licenceKeyValidator = licenceKeyValidator;
    }

    public async Task<LicenceStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var lastHeartbeatUtc = await _stateStore.GetLastHeartbeatUtcAsync(cancellationToken);
        var storedKey = await _stateStore.GetActivationKeyAsync(cancellationToken);
        return BuildStatus(lastHeartbeatUtc, DecodeEntitlements(storedKey));
    }

    public async Task<LicenceStatus> ActivateAsync(string licenceKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(licenceKey))
        {
            return new LicenceStatus
            {
                IsActive = false,
                State = "invalid",
                AllowsAdminWrite = false,
                ReasonCode = "licence.invalid_key"
            };
        }

        var validation = _licenceKeyValidator.Validate(licenceKey);
        if (!validation.IsValid)
        {
            return new LicenceStatus
            {
                IsActive = false,
                State = "invalid",
                AllowsAdminWrite = false,
                ReasonCode = validation.ReasonCode ?? "licence.invalid_key"
            };
        }

        await _stateStore.SetActivationKeyAsync(licenceKey.Trim(), cancellationToken);
        await _stateStore.SetLastHeartbeatUtcAsync(_clock.UtcNow, cancellationToken);
        return BuildStatus(_clock.UtcNow, validation);
    }

    public async Task<LicenceStatus> HeartbeatAsync(CancellationToken cancellationToken)
    {
        var storedKey = await _stateStore.GetActivationKeyAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(storedKey))
            return await GetStatusAsync(cancellationToken);

        var validation = _licenceKeyValidator.Validate(storedKey);
        if (!validation.IsValid)
            return await GetStatusAsync(cancellationToken);

        await _stateStore.SetLastHeartbeatUtcAsync(_clock.UtcNow, cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    private LicenceStatus BuildStatus(DateTimeOffset? lastHeartbeatUtc, LicenceKeyValidationResult? entitlements)
    {
        var entitled = entitlements is { IsValid: true };
        var tier = entitled ? entitlements!.Tier : LicenceTier.Unknown;
        var marketplace = entitled && entitlements!.MarketplaceModuleEntitlement;
        var workshop = entitled && entitlements!.WorkshopPortalEntitlement;
        var fleet = entitled && entitlements!.FleetPortalEntitlement;
        var dealer = entitled && entitlements!.DealerPortalEntitlement;

        if (!lastHeartbeatUtc.HasValue)
        {
            return new LicenceStatus
            {
                IsActive = false,
                State = "inactive",
                AllowsAdminWrite = false,
                ReasonCode = "licence.not_activated",
                Tier = LicenceTier.Unknown,
                MarketplaceModuleEntitlement = false,
                WorkshopPortalEntitlement = false,
                FleetPortalEntitlement = false,
                DealerPortalEntitlement = false
            };
        }

        var age = _clock.UtcNow - lastHeartbeatUtc.Value;
        if (age <= CheckEngineLicenceGate.GracePeriod)
        {
            return new LicenceStatus
            {
                IsActive = true,
                State = "active",
                LastHeartbeatUtc = lastHeartbeatUtc,
                AllowsAdminWrite = true,
                Tier = tier,
                MarketplaceModuleEntitlement = marketplace,
                WorkshopPortalEntitlement = workshop,
                FleetPortalEntitlement = fleet,
                DealerPortalEntitlement = dealer
            };
        }

        return new LicenceStatus
        {
            IsActive = false,
            State = "read_only",
            LastHeartbeatUtc = lastHeartbeatUtc,
            AllowsAdminWrite = false,
            ReasonCode = "licence.grace_expired",
            Tier = tier,
            MarketplaceModuleEntitlement = marketplace,
            WorkshopPortalEntitlement = workshop,
            FleetPortalEntitlement = fleet,
            DealerPortalEntitlement = dealer
        };
    }

    private LicenceKeyValidationResult? DecodeEntitlements(string? storedKey)
    {
        if (string.IsNullOrWhiteSpace(storedKey))
            return null;

        var validation = _licenceKeyValidator.Validate(storedKey);
        return validation.IsValid ? validation : null;
    }
}
