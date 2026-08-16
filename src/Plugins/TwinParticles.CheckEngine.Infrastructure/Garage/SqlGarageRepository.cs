using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Nop.Data;
using TwinParticles.CheckEngine.Domain.Garage;
using TwinParticles.CheckEngine.Infrastructure.Data;

namespace TwinParticles.CheckEngine.Infrastructure.Garage;

public sealed class SqlGarageRepository : IGarageRepository
{
    private readonly INopDataProvider _dataProvider;
    private readonly IGarageVinProtector _vinProtector;

    public SqlGarageRepository(INopDataProvider dataProvider, IGarageVinProtector vinProtector)
    {
        _dataProvider = dataProvider;
        _vinProtector = vinProtector;
    }

    public async Task<Domain.Garage.Garage> GetOrCreateAsync(int customerId, CancellationToken cancellationToken)
    {
        var existing = await GetByCustomerIdAsync(customerId, cancellationToken);
        if (existing is not null)
            return existing;

        var now = DateTime.UtcNow;
        var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(
            @"INSERT INTO TP_CE_Garage (CustomerId, ActiveGarageVehicleId, CreatedUtc, UpdatedUtc)
VALUES (@customerId, NULL, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId(),
            new DataParameter("customerId", customerId),
            new DataParameter("createdUtc", now),
            new DataParameter("updatedUtc", now));

        return new Domain.Garage.Garage
        {
            Id = inserted.Single().Value,
            CustomerId = customerId,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }

    public async Task<Domain.Garage.Garage?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var garageRows = await _dataProvider.QueryAsync<GarageRow>(
            @"SELECT Id, CustomerId, ActiveGarageVehicleId, CreatedUtc, UpdatedUtc
FROM TP_CE_Garage WHERE CustomerId = @customerId",
            new DataParameter("customerId", customerId));

        var garageRow = garageRows.FirstOrDefault();
        if (garageRow is null)
            return null;

        var vehicles = await _dataProvider.QueryAsync<GarageVehicle>(
            @"SELECT Id, GarageId, VehicleConfigurationId, Vin, Label, IsActive, CreatedUtc
FROM TP_CE_GarageVehicle WHERE GarageId = @garageId ORDER BY Id",
            new DataParameter("garageId", garageRow.Id));
        foreach (var vehicle in vehicles)
        {
            var storedVin = vehicle.Vin;
            vehicle.Vin = _vinProtector.Unprotect(storedVin);

            // Opportunistically migrate legacy plaintext on first authenticated read/export. New and
            // subsequently saved VINs are always protected before INSERT.
            var protectedVin = _vinProtector.Protect(storedVin);
            if (!string.IsNullOrWhiteSpace(storedVin) &&
                !string.Equals(storedVin, protectedVin, StringComparison.Ordinal))
            {
                await _dataProvider.ExecuteNonQueryAsync(
                    "UPDATE TP_CE_GarageVehicle SET Vin=@vin WHERE Id=@id",
                    new DataParameter("vin", protectedVin),
                    new DataParameter("id", vehicle.Id));
            }
        }

        var oems = await _dataProvider.QueryAsync<GarageOem>(
            @"SELECT Id, GarageId, OemNumberId, DisplayNumber, CreatedUtc
FROM TP_CE_GarageOem WHERE GarageId = @garageId ORDER BY Id",
            new DataParameter("garageId", garageRow.Id));

        return new Domain.Garage.Garage
        {
            Id = garageRow.Id,
            CustomerId = garageRow.CustomerId,
            ActiveGarageVehicleId = garageRow.ActiveGarageVehicleId,
            CreatedUtc = garageRow.CreatedUtc,
            UpdatedUtc = garageRow.UpdatedUtc,
            Vehicles = vehicles.ToList(),
            Oems = oems.ToList()
        };
    }

    public async Task SaveAsync(Domain.Garage.Garage garage, CancellationToken cancellationToken)
    {
        garage.UpdatedUtc = DateTime.UtcNow;

        if (garage.Id <= 0)
        {
            var inserted = await _dataProvider.QueryAsync<ScalarIntRow>(
                @"INSERT INTO TP_CE_Garage (CustomerId, ActiveGarageVehicleId, CreatedUtc, UpdatedUtc)
VALUES (@customerId, @activeGarageVehicleId, @createdUtc, @updatedUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("customerId", garage.CustomerId),
                new DataParameter("activeGarageVehicleId", garage.ActiveGarageVehicleId),
                new DataParameter("createdUtc", garage.CreatedUtc == default ? garage.UpdatedUtc : garage.CreatedUtc),
                new DataParameter("updatedUtc", garage.UpdatedUtc));

            garage.Id = inserted.Single().Value;
        }
        else
        {
            await _dataProvider.ExecuteNonQueryAsync(
                @"UPDATE TP_CE_Garage
SET ActiveGarageVehicleId = @activeGarageVehicleId,
    UpdatedUtc = @updatedUtc
WHERE Id = @id",
                new DataParameter("activeGarageVehicleId", garage.ActiveGarageVehicleId),
                new DataParameter("updatedUtc", garage.UpdatedUtc),
                new DataParameter("id", garage.Id));
        }

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_GarageOem WHERE GarageId = @garageId",
            new DataParameter("garageId", garage.Id));
        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_GarageVehicle WHERE GarageId = @garageId",
            new DataParameter("garageId", garage.Id));

        foreach (var vehicle in garage.Vehicles)
        {
            var insertedVehicle = await _dataProvider.QueryAsync<ScalarIntRow>(
                @"INSERT INTO TP_CE_GarageVehicle
(GarageId, VehicleConfigurationId, Vin, Label, IsActive, CreatedUtc)
VALUES
(@garageId, @vehicleConfigurationId, @vin, @label, @isActive, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("garageId", garage.Id),
                new DataParameter("vehicleConfigurationId", vehicle.VehicleConfigurationId),
                new DataParameter("vin", _vinProtector.Protect(vehicle.Vin)),
                new DataParameter("label", vehicle.Label),
                new DataParameter("isActive", vehicle.IsActive),
                new DataParameter("createdUtc", vehicle.CreatedUtc == default ? garage.UpdatedUtc : vehicle.CreatedUtc));

            vehicle.Id = insertedVehicle.Single().Value;
            vehicle.GarageId = garage.Id;
        }

        if (garage.ActiveGarageVehicleId.HasValue)
        {
            var active = garage.Vehicles.FirstOrDefault(v => v.IsActive);
            if (active is not null)
            {
                garage.ActiveGarageVehicleId = active.Id;
                await _dataProvider.ExecuteNonQueryAsync(
                    "UPDATE TP_CE_Garage SET ActiveGarageVehicleId = @activeGarageVehicleId WHERE Id = @id",
                    new DataParameter("activeGarageVehicleId", garage.ActiveGarageVehicleId),
                    new DataParameter("id", garage.Id));
            }
        }

        foreach (var oem in garage.Oems)
        {
            var insertedOem = await _dataProvider.QueryAsync<ScalarIntRow>(
                @"INSERT INTO TP_CE_GarageOem (GarageId, OemNumberId, DisplayNumber, CreatedUtc)
VALUES (@garageId, @oemNumberId, @displayNumber, @createdUtc);
" + CheckEngineSql.SelectInsertedIntId(),
                new DataParameter("garageId", garage.Id),
                new DataParameter("oemNumberId", oem.OemNumberId),
                new DataParameter("displayNumber", oem.DisplayNumber),
                new DataParameter("createdUtc", oem.CreatedUtc == default ? garage.UpdatedUtc : oem.CreatedUtc));

            oem.Id = insertedOem.Single().Value;
            oem.GarageId = garage.Id;
        }
    }

    public async Task<bool> DeleteByCustomerIdAsync(int customerId, CancellationToken cancellationToken)
    {
        var garageRows = await _dataProvider.QueryAsync<GarageRow>(
            "SELECT Id, CustomerId, ActiveGarageVehicleId, CreatedUtc, UpdatedUtc FROM TP_CE_Garage WHERE CustomerId = @customerId",
            new DataParameter("customerId", customerId));
        var garage = garageRows.FirstOrDefault();
        if (garage is null)
            return false;

        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_GarageOem WHERE GarageId=@garageId",
            new DataParameter("garageId", garage.Id));
        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_GarageVehicle WHERE GarageId=@garageId",
            new DataParameter("garageId", garage.Id));
        await _dataProvider.ExecuteNonQueryAsync(
            "DELETE FROM TP_CE_Garage WHERE Id=@garageId",
            new DataParameter("garageId", garage.Id));

        return true;
    }

    private sealed class ScalarIntRow
    {
        public int Value { get; set; }
    }

    private sealed class GarageRow
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? ActiveGarageVehicleId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
