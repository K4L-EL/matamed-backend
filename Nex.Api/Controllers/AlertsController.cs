using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController(IAlertService alertService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool? unreadOnly = null)
        => Ok(await alertService.GetAllAsync(unreadOnly));

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkAsRead(string id)
    {
        var alert = await alertService.MarkAsReadAsync(id);
        return alert is null ? NotFound() : Ok(alert);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest request)
    {
        var alert = await alertService.CreateAsync(request);
        return Created($"/api/alerts/{alert.Id}", alert);
    }
}
