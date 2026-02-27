using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubScreeningService : IScreeningService
{
    private static int _nextId = 13;

    private static readonly List<ScreeningCompliance> Compliance =
    [
        new("ICU-A", 14, 11, 3, 0.786),
        new("ICU-B", 12, 10, 2, 0.833),
        new("Ward 2A", 20, 19, 1, 0.950),
        new("Ward 3B", 18, 14, 4, 0.778),
        new("Ward 4C", 16, 13, 3, 0.813),
        new("Surgical", 10, 10, 0, 1.0),
        new("Emergency", 22, 18, 4, 0.818),
        new("Neonatal", 8, 8, 0, 1.0)
    ];

    private static readonly List<ScreeningRecord> Records =
    [
        new("SCR001", "P001", "James Wilson", "ICU-A", "MRSA", "Overdue", DateTime.UtcNow.AddDays(-3), null, null),
        new("SCR002", "P002", "Sarah Chen", "Ward 3B", "MRSA", "Completed", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2), "Negative"),
        new("SCR003", "P003", "Ahmed Hassan", "ICU-B", "CPE", "Overdue", DateTime.UtcNow.AddDays(-1), null, null),
        new("SCR004", "P004", "Maria Garcia", "Ward 2A", "MRSA", "Completed", DateTime.UtcNow.AddDays(-4), DateTime.UtcNow.AddDays(-4), "Negative"),
        new("SCR005", "P005", "David Thompson", "Ward 4C", "VRE", "Completed", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-4), "Positive"),
        new("SCR006", "P007", "Robert Kim", "ICU-A", "MRSA", "Overdue", DateTime.UtcNow.AddDays(-2), null, null),
        new("SCR007", "P008", "Emma Brown", "Ward 3B", "C. difficile", "Completed", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-3), "Positive"),
        new("SCR008", "P011", "Michael O'Brien", "ICU-B", "Candida auris", "Pending", DateTime.UtcNow.AddDays(1), null, null),
        new("SCR009", "P009", "Thomas Wright", "Ward 2A", "MRSA", "Completed", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1), "Negative"),
        new("SCR010", "P006", "Lisa Patel", "Surgical", "MRSA", "Completed", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-1), "Negative"),
        new("SCR011", "P010", "Fatima Al-Rashid", "Surgical", "MRSA", "Completed", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1), "Negative"),
        new("SCR012", "P012", "Yuki Tanaka", "Ward 4C", "C. difficile", "Overdue", DateTime.UtcNow.AddDays(-2), null, null)
    ];

    public Task<List<ScreeningCompliance>> GetComplianceAsync()
        => Task.FromResult(Compliance);

    public Task<List<ScreeningRecord>> GetRecordsAsync(string? ward = null, string? status = null)
    {
        var result = Records.AsEnumerable();
        if (!string.IsNullOrEmpty(ward))
            result = result.Where(r => r.Ward.Equals(ward, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(status))
            result = result.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result.ToList());
    }

    public Task<ScreeningRecord> CreateRecordAsync(CreateScreeningRecordRequest request)
    {
        var id = $"SCR{Interlocked.Increment(ref _nextId):D3}";
        var record = new ScreeningRecord(
            id, request.PatientId, request.PatientName, request.Ward,
            request.ScreeningType, "Pending", request.DueDate, null, null
        );
        Records.Add(record);
        return Task.FromResult(record);
    }
}
