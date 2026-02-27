using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IPatientService
{
    Task<List<Patient>> GetAllAsync(string? ward = null, string? status = null);
    Task<Patient?> GetByIdAsync(string id);
    Task<PatientRisk?> GetRiskAsync(string id);
    Task<Patient> CreateAsync(CreatePatientRequest request);
}
