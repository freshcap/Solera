using Microsoft.AspNetCore.Mvc;
using VehicleApi.Data;

namespace VehicleApi.Controllers;

[Route("vehicles")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleRepository _vehicleRepository;

    public VehicleController(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    [HttpGet("aggregation/year")]
    public async Task<ActionResult<IEnumerable<VehicleYearAggregation>>> GetYearAggregation()
    {
        var results = await _vehicleRepository.GetVehicleAggregationByYear();
        return Ok(results);
    }
}
