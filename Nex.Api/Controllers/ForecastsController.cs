using Microsoft.AspNetCore.Mvc;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ForecastsController(IForecastService forecastService) : ControllerBase
{
    [HttpGet("risk-scores")]
    public async Task<IActionResult> GetRiskScores()
        => Ok(await forecastService.GetRiskScoresAsync());

    [HttpGet("location-risks")]
    public async Task<IActionResult> GetLocationRisks()
        => Ok(await forecastService.GetLocationRisksAsync());

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 14)
        => Ok(await forecastService.GetTrendsAsync(days));
}
