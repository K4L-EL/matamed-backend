using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubOutbreakService : IOutbreakService
{
    private static int _nextId = StubData.Outbreaks.Count + 1;

    public Task<List<Outbreak>> GetAllAsync(string? status = null)
    {
        var outbreaks = StubData.Outbreaks.AsEnumerable();

        if (!string.IsNullOrEmpty(status))
            outbreaks = outbreaks.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(outbreaks.ToList());
    }

    public Task<OutbreakDetail?> GetByIdAsync(string id)
    {
        var outbreak = StubData.Outbreaks.FirstOrDefault(o => o.Id == id);
        if (outbreak is null) return Task.FromResult<OutbreakDetail?>(null);

        var detail = new OutbreakDetail(
            outbreak,
            ["ICU-A", "ICU-B", "Ward 3B"],
            [
                new OutbreakTimeline(outbreak.DetectedAt, "Index Case", "First case identified through routine screening"),
                new OutbreakTimeline(outbreak.DetectedAt.AddDays(1), "Investigation Opened", "IPC team notified and investigation initiated"),
                new OutbreakTimeline(outbreak.DetectedAt.AddDays(2), "Contact Tracing", "Exposed patients identified and screened"),
                new OutbreakTimeline(outbreak.DetectedAt.AddDays(3), "Enhanced Measures", "Deep cleaning and enhanced precautions implemented")
            ],
            [
                "Screen all contacts within 48 hours of exposure",
                "Implement contact precautions for all positive cases",
                "Enhanced environmental cleaning in affected wards",
                "Review antimicrobial prescribing in affected areas"
            ]
        );
        return Task.FromResult<OutbreakDetail?>(detail);
    }

    public Task<Outbreak> CreateAsync(CreateOutbreakRequest request)
    {
        var id = $"OB{Interlocked.Increment(ref _nextId):D3}";
        var outbreak = new Outbreak(
            id, request.Organism, request.Location, DateTime.UtcNow,
            null, "Suspected", request.AffectedPatients, request.Severity, "Initiated"
        );
        StubData.Outbreaks.Add(outbreak);
        return Task.FromResult(outbreak);
    }
}
