using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubPatientService : IPatientService
{
    private static int _nextId = StubData.Patients.Count + 1;

    public Task<List<Patient>> GetAllAsync(string? ward = null, string? status = null)
    {
        var patients = StubData.Patients.AsEnumerable();

        if (!string.IsNullOrEmpty(ward))
            patients = patients.Where(p => p.Ward.Equals(ward, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(status))
            patients = patients.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(patients.ToList());
    }

    public Task<Patient?> GetByIdAsync(string id)
    {
        return Task.FromResult(StubData.Patients.FirstOrDefault(p => p.Id == id));
    }

    public Task<PatientRisk?> GetRiskAsync(string id)
    {
        var patient = StubData.Patients.FirstOrDefault(p => p.Id == id);
        if (patient is null) return Task.FromResult<PatientRisk?>(null);

        var riskLevel = patient.RiskScore switch
        {
            > 0.75 => "Critical",
            > 0.5 => "High",
            > 0.25 => "Medium",
            _ => "Low"
        };

        var risk = new PatientRisk(
            patient.Id,
            patient.RiskScore,
            [
                new RiskFactor("Length of Stay", 0.25, "Extended hospitalization increases exposure risk"),
                new RiskFactor("Invasive Devices", 0.30, "Central line and urinary catheter present"),
                new RiskFactor("Immunosuppression", 0.20, "Current immunosuppressive therapy"),
                new RiskFactor("Prior Infection", 0.15, "History of healthcare-associated infection"),
                new RiskFactor("Age", 0.10, "Age-related susceptibility factor")
            ],
            riskLevel
        );
        return Task.FromResult<PatientRisk?>(risk);
    }

    public Task<Patient> CreateAsync(CreatePatientRequest request)
    {
        var id = $"P{Interlocked.Increment(ref _nextId):D3}";
        var patient = new Patient(
            id, request.Name, request.Age, request.Gender, request.Ward,
            request.BedNumber, DateTime.UtcNow, request.Status, 0.15, 0, []
        );
        StubData.Patients.Add(patient);
        return Task.FromResult(patient);
    }
}
