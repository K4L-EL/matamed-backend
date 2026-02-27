using Microsoft.AspNetCore.Mvc;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(await dashboardService.GetSummaryAsync());

    [HttpGet("trends")]
    public async Task<IActionResult> GetTrends([FromQuery] int days = 30)
        => Ok(await dashboardService.GetTrendsAsync(days));
}
