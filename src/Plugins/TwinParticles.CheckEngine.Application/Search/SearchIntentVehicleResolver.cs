using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinParticles.CheckEngine.Application.Vehicle.Admin;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Queries;
using TwinParticles.CheckEngine.Application.Vehicle.Aliases.Services;
using TwinParticles.CheckEngine.Domain.Search;

namespace TwinParticles.CheckEngine.Application.Search;

/// <summary>
/// Resolves structured NL intent (make/model/year) to a vehicle configuration id via alias search.
/// </summary>
public sealed class SearchIntentVehicleResolver
{
    private readonly VehicleAliasApplicationService _aliasService;
    private readonly VehicleAdminService? _vehicleAdminService;

    public SearchIntentVehicleResolver(
        VehicleAliasApplicationService aliasService,
        VehicleAdminService? vehicleAdminService = null)
    {
        _aliasService = aliasService;
        _vehicleAdminService = vehicleAdminService;
    }

    public async Task<int?> ResolveConfigurationIdAsync(SearchIntent intent, CancellationToken cancellationToken)
    {
        if (intent.VehicleConfigurationId is > 0)
            return intent.VehicleConfigurationId;

        var term = BuildSearchTerm(intent);
        if (string.IsNullOrWhiteSpace(term))
            return null;

        var aliases = await _aliasService.SearchAsync(new SearchVehicleAliasesQuery
        {
            Term = term,
            Locale = intent.Locale,
            Take = 12
        }, cancellationToken);

        var configurations = aliases
            .Where(alias => string.Equals(alias.NodeType, "configuration", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (configurations.Count == 0)
            return null;

        if (configurations.Count == 1)
            return configurations[0].NodeId;

        if (intent.ModelYear is int year && _vehicleAdminService is not null)
        {
            foreach (var configuration in configurations)
            {
                var entity = await _vehicleAdminService.GetConfigurationByIdAsync(configuration.NodeId, cancellationToken);
                if (entity is null)
                    continue;

                if (entity.ProductionFromYear <= year &&
                    (entity.ProductionToYear is null || entity.ProductionToYear >= year))
                {
                    return configuration.NodeId;
                }
            }
        }

        return configurations[0].NodeId;
    }

    private static string BuildSearchTerm(SearchIntent intent)
    {
        if (!string.IsNullOrWhiteSpace(intent.Model))
        {
            return string.Join(' ', new[] { intent.Make, intent.Model }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        if (!string.IsNullOrWhiteSpace(intent.Make))
            return intent.Make.Trim();

        return string.Empty;
    }
}
