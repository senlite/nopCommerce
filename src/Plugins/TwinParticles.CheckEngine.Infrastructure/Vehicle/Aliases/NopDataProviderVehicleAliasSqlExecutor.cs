using System.Collections.Generic;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Vehicle.Aliases;

public sealed class NopDataProviderVehicleAliasSqlExecutor : IVehicleAliasSqlExecutor
{
    private readonly INopDataProvider _dataProvider;

    public NopDataProviderVehicleAliasSqlExecutor(INopDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public Task<int> ExecuteNonQueryAsync(string sql, params DataParameter[] parameters)
    {
        return _dataProvider.ExecuteNonQueryAsync(sql, parameters);
    }

    public Task<IList<T>> QueryAsync<T>(string sql, params DataParameter[] parameters)
    {
        return _dataProvider.QueryAsync<T>(sql, parameters);
    }
}
