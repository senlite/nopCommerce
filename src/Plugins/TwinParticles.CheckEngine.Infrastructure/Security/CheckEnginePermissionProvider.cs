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

    public static readonly PermissionConfig ManageCheckEngineWorkshop = new(
        "Admin area. Manage Check Engine workshop portal",
        "ManageCheckEngineWorkshop",
        "Check Engine",
        NopCustomerDefaults.AdministratorsRoleName);

    public static readonly PermissionConfig ManageCheckEngineFleet = new(
        "Admin area. Manage Check Engine fleet portal",
        "ManageCheckEngineFleet",
        "Check Engine",
        NopCustomerDefaults.AdministratorsRoleName);

    public static readonly PermissionConfig ManageCheckEngineDealer = new(
        "Admin area. Manage Check Engine dealer portal",
        "ManageCheckEngineDealer",
        "Check Engine",
        NopCustomerDefaults.AdministratorsRoleName);

    public IList<PermissionConfig> AllConfigs =>
    [
        ManageCheckEngine,
        ManageCheckEngineWorkshop,
        ManageCheckEngineFleet,
        ManageCheckEngineDealer
    ];
}
