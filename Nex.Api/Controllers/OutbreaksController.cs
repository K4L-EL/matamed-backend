using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OutbreaksController(IOutbreakService outbreakService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        => Ok(await outbreakService.GetAllAsync(status));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var detail = await outbreakService.GetByIdAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOutbreakRequest request)
    {
        var outbreak = await outbreakService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = outbreak.Id }, outbreak);
    }
}
