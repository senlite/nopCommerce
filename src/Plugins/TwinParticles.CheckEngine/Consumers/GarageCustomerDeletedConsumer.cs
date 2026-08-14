using System.Threading;
using System.Threading.Tasks;
using Nop.Core.Domain.Gdpr;
using Nop.Services.Events;
using TwinParticles.CheckEngine.Application.Garage;

namespace TwinParticles.CheckEngine.Consumers;

/// <summary>
/// Ensures nopCommerce permanent customer erasure also removes Check Engine garage data.
/// </summary>
public sealed class GarageCustomerDeletedConsumer : IConsumer<CustomerPermanentlyDeleted>
{
    private readonly GaragePrivacyService _privacyService;

    public GarageCustomerDeletedConsumer(GaragePrivacyService privacyService)
    {
        _privacyService = privacyService;
    }

    public Task HandleEventAsync(CustomerPermanentlyDeleted eventMessage)
        => _privacyService.EraseAsync(
            eventMessage.CustomerId,
            "system:customer-permanent-delete",
            CancellationToken.None);
}
