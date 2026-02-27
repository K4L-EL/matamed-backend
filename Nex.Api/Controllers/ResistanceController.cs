using Microsoft.AspNetCore.Mvc;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResistanceController(IResistanceService resistanceService) : ControllerBase
{
    [HttpGet("summaries")]
    public async Task<IActionResult> GetSummaries()
        => Ok(await resistanceService.GetSummariesAsync());

    [HttpGet("prescriptions")]
    public async Task<IActionResult> GetPrescriptions([FromQuery] string? antibiotic = null)
        => Ok(await resistanceService.GetPrescriptionsAsync(antibiotic));
}
