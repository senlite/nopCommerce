using System.Threading;
using System.Threading.Tasks;

namespace TwinParticles.CheckEngine.Domain.Tenancy;

public interface ITenantAccessor
{
    TenantContext Current { get; }

    void SetCurrent(TenantContext context);
}
