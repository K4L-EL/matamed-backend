using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipelinesController(IPipelineService pipelineService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await pipelineService.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var pipeline = await pipelineService.GetByIdAsync(id);
        return pipeline is null ? NotFound() : Ok(pipeline);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePipelineRequest request)
    {
        var pipeline = await pipelineService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = pipeline.Id }, pipeline);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreatePipelineRequest request)
    {
        var pipeline = await pipelineService.UpdateAsync(id, request);
        return pipeline is null ? NotFound() : Ok(pipeline);
    }
}
