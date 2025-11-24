using Microsoft.AspNetCore.Mvc;
using VehicleApi.Clients;
using VehicleApi.Data;

namespace VehicleApi.Controllers;

[Route("test")]
public class TestController : ControllerBase
{
    private IVehicleRepository _vehicleRepository;
    private IClaudeClient _claudeClient;

    public TestController(IVehicleRepository vehicleRepository, IClaudeClient claudeClient)
    {
        _vehicleRepository = vehicleRepository;
        _claudeClient = claudeClient;
    }

    [HttpGet("data")]
    public async Task<ActionResult<IEnumerable<VehicleData>>> TestData()
    {
        var vehicles = await _vehicleRepository.TestGet();
        return Ok(vehicles);
    }

    [HttpPost("claude")]
    public async Task<ActionResult<IEnumerable<VehicleData>>> TestClaude([FromBody] ClaudeRequest request)
    {
        var response = await _claudeClient.GetCompletion(request.Prompt);
        return Ok(response);
    }
}
