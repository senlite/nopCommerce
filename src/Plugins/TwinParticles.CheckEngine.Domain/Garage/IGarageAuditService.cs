using System.Collections.Generic;

namespace TwinParticles.CheckEngine.Domain.Garage;

public interface IGarageAuditService
{
    void RecordAdminView(int customerId);

    IReadOnlyList<int> GetViewedCustomerIds();
}
