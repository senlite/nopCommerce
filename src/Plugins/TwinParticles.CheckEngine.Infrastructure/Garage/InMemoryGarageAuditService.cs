using System.Collections.Generic;
using TwinParticles.CheckEngine.Domain.Garage;

namespace TwinParticles.CheckEngine.Infrastructure.Garage;

public sealed class InMemoryGarageAuditService : IGarageAuditService
{
    private readonly List<int> _viewedCustomerIds = [];

    public void RecordAdminView(int customerId)
    {
        _viewedCustomerIds.Add(customerId);
    }

    public IReadOnlyList<int> GetViewedCustomerIds()
    {
        return _viewedCustomerIds;
    }
}
