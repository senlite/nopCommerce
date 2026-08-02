using System.Collections.Generic;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Security;

namespace TwinParticles.CheckEngine.Security;

public sealed class CheckEnginePermissionProvider : IPermissionProvider
{
    public static readonly PermissionRecord ManageCheckEngine = new()
    {
        Name = "Admin area. Manage Check Engine",
        SystemName = "ManageCheckEngine",
        Category = "Check Engine"
    };

    public IEnumerable<PermissionRecord> GetPermissions()
    {
        return new[]
        {
            ManageCheckEngine
        };
    }

    public HashSet<(string systemRoleName, PermissionRecord[] permissions)> GetDefaultPermissions()
    {
        return new HashSet<(string systemRoleName, PermissionRecord[] permissions)>
        {
            (
                NopCustomerDefaults.AdministratorsRoleName,
                new[]
                {
                    ManageCheckEngine
                }
            )
        };
    }
}
