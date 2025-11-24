using Dapper;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace VehicleApi.Data;

public interface IVehicleRepository
{
    Task<IEnumerable<VehicleData>> TestGet();
}

public class VehicleRepository : IVehicleRepository
{
    private string _connectionString;

    public VehicleRepository(IOptions<AppSettings> settings)
    {
        _connectionString = settings.Value.DbConnectionString;
    }

    public async Task<IEnumerable<VehicleData>> TestGet()
    {
        using var connection = new SqlConnection(_connectionString);
        var vehicles = await connection.QueryAsync<VehicleData>("SELECT TOP 100 * FROM VehicleData");
        
        return vehicles;
    }
}
