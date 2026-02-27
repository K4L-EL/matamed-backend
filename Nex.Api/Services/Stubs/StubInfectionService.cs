using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubInfectionService : IInfectionService
{
    private static int _nextId = StubData.Infections.Count + 1;

    public Task<List<Infection>> GetAllAsync(string? status = null, string? ward = null)
    {
        var infections = StubData.Infections.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
            infections = infections.Where(i => i.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(ward))
            infections = infections.Where(i => i.Ward.Equals(ward, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(infections.ToList());
    }

    public Task<InfectionDetail?> GetByIdAsync(string id)
    {
        var infection = StubData.Infections.FirstOrDefault(i => i.Id == id);
        if (infection is null) return Task.FromResult<InfectionDetail?>(null);

        var detail = new InfectionDetail(
            infection,
            ["Central venous catheter", "Prolonged ICU stay", "Immunosuppression", "Recent antibiotic use"],
            [
                new InfectionEvent(infection.DetectedAt.AddHours(-12), "Positive blood culture collected", "Sample"),
                new InfectionEvent(infection.DetectedAt, "Infection confirmed by microbiology", "Diagnosis"),
                new InfectionEvent(infection.DetectedAt.AddHours(2), "Contact precautions initiated", "Intervention"),
                new InfectionEvent(infection.DetectedAt.AddHours(4), "Antibiotic therapy started", "Treatment"),
            ]
        );
        return Task.FromResult<InfectionDetail?>(detail);
    }

    public Task<Infection> CreateAsync(CreateInfectionRequest request)
    {
        var id = $"INF{Interlocked.Increment(ref _nextId):D3}";
        var patient = StubData.Patients.FirstOrDefault(p => p.Id == request.PatientId);
        var infection = new Infection(
            id, request.PatientId, patient?.Name ?? "Unknown",
            request.Organism, request.Type, request.Location,
            request.Ward, "Active", DateTime.UtcNow, null,
            request.Severity, request.IsHai
        );
        StubData.Infections.Add(infection);
        return Task.FromResult(infection);
    }
}
