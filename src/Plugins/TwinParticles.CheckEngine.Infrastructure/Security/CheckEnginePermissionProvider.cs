using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace TwinParticles.CheckEngine.Security;

public sealed class CheckEnginePermissionProvider : IPermissionConfigManager
{
    public static readonly PermissionConfig ManageCheckEngine = new(
        "Admin area. Manage Check Engine",
        "ManageCheckEngine",
        "Check Engine",
        NopCustomerDefaults.AdministratorsRoleName);

    public IList<PermissionConfig> AllConfigs => [ManageCheckEngine];
}
