using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfectionsController(IInfectionService infectionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] string? ward = null)
        => Ok(await infectionService.GetAllAsync(status, ward));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var detail = await infectionService.GetByIdAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInfectionRequest request)
    {
        var infection = await infectionService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = infection.Id }, infection);
    }
}
