using System.Collections.Generic;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public interface IVehicleAliasSqlExecutor
{
    Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] parameters);

    Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters);
}
