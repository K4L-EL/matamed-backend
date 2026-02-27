using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScreeningController(IScreeningService screeningService) : ControllerBase
{
    [HttpGet("compliance")]
    public async Task<IActionResult> GetCompliance()
        => Ok(await screeningService.GetComplianceAsync());

    [HttpGet("records")]
    public async Task<IActionResult> GetRecords([FromQuery] string? ward = null, [FromQuery] string? status = null)
        => Ok(await screeningService.GetRecordsAsync(ward, status));

    [HttpPost("records")]
    public async Task<IActionResult> CreateRecord([FromBody] CreateScreeningRecordRequest request)
    {
        var record = await screeningService.CreateRecordAsync(request);
        return Created($"/api/screening/records/{record.Id}", record);
    }
}
