using Microsoft.AspNetCore.Mvc;
using Nex.Api.Models;
using Nex.Api.Services;

namespace Nex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(IPatientService patientService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? ward = null, [FromQuery] string? status = null)
        => Ok(await patientService.GetAllAsync(ward, status));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var patient = await patientService.GetByIdAsync(id);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet("{id}/risk")]
    public async Task<IActionResult> GetRisk(string id)
    {
        var risk = await patientService.GetRiskAsync(id);
        return risk is null ? NotFound() : Ok(risk);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var patient = await patientService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }
}
