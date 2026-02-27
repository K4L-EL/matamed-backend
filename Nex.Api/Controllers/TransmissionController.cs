using Microsoft.AspNetCore.Mvc;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransmissionController(ITransmissionService transmissionService) : ControllerBase
{
    [HttpGet("network")]
    public async Task<IActionResult> GetNetwork([FromQuery] string? organism = null)
        => Ok(await transmissionService.GetNetworkAsync(organism));
}
