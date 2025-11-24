using Dapper;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

namespace VehicleApi.Data;

public interface IVehicleRepository
{
    Task<IEnumerable<TestSumVehicle>> TestGet();
}

public class VehicleRepository : IVehicleRepository
{
    private string _connectionString;

    public VehicleRepository(IOptions<AppSettings> settings)
    {
        _connectionString = settings.Value.DbConnectionString;
    }

    public async Task<IEnumerable<TestSumVehicle>> TestGet()
    {
        using var connection = new SqlConnection(_connectionString);
        var vehicles = await connection.QueryAsync<TestSumVehicle>("SELECT top 50 Make, Model, sum(value) as Total FROM VehicleData where year = 2025 group by make, model order by total desc");
        
        return vehicles;
    }
}
