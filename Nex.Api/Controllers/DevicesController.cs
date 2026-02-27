using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(IDeviceService deviceService) : ControllerBase
{
    [HttpGet("summaries")]
    public async Task<IActionResult> GetSummaries()
        => Ok(await deviceService.GetSummariesAsync());

    [HttpGet("infections")]
    public async Task<IActionResult> GetInfections([FromQuery] string? deviceType = null)
        => Ok(await deviceService.GetInfectionsAsync(deviceType));

    [HttpPost("infections")]
    public async Task<IActionResult> CreateInfection([FromBody] CreateDeviceInfectionRequest request)
    {
        var infection = await deviceService.CreateInfectionAsync(request);
        return Created($"/api/devices/infections/{infection.Id}", infection);
    }
}
