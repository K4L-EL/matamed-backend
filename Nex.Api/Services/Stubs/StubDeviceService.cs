using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubDeviceService : IDeviceService
{
    private static int _nextId = 10;

    private static readonly List<DeviceSummary> Summaries =
    [
        new("Central Venous Catheter", 34, 4, 0.118, 8.5),
        new("Urinary Catheter", 52, 3, 0.058, 6.2),
        new("Peripheral IV", 128, 2, 0.016, 3.8),
        new("Ventilator", 18, 3, 0.167, 5.4),
        new("Surgical Drain", 22, 1, 0.045, 4.1),
    ];

    private static readonly List<DeviceInfection> Infections =
    [
        new("DI001", "P001", "James Wilson", "Central Venous Catheter", "MRSA", "ICU-A", DateTime.UtcNow.AddDays(-14), DateTime.UtcNow.AddDays(-6), 8, "Active"),
        new("DI002", "P002", "Sarah Chen", "Urinary Catheter", "E. coli", "Ward 3B", DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddDays(-2), 6, "Active"),
        new("DI003", "P003", "Ahmed Hassan", "Ventilator", "Klebsiella pneumoniae", "ICU-B", DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(-10), 5, "Active"),
        new("DI004", "P007", "Robert Kim", "Ventilator", "Pseudomonas aeruginosa", "ICU-A", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-5), 5, "Active"),
        new("DI005", "P011", "Michael O'Brien", "Central Venous Catheter", "MRSA", "ICU-B", DateTime.UtcNow.AddDays(-18), DateTime.UtcNow.AddDays(-9), 9, "Active"),
        new("DI006", "P005", "David Thompson", "Surgical Drain", "VRE", "Ward 4C", DateTime.UtcNow.AddDays(-12), DateTime.UtcNow.AddDays(-7), 5, "Active"),
        new("DI007", "P011", "Michael O'Brien", "Central Venous Catheter", "Candida auris", "ICU-B", DateTime.UtcNow.AddDays(-18), DateTime.UtcNow.AddDays(-3), 15, "Active"),
        new("DI008", "P003", "Ahmed Hassan", "Ventilator", "Pseudomonas aeruginosa", "ICU-B", DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddDays(-8), 7, "Resolved"),
        new("DI009", "P008", "Emma Brown", "Peripheral IV", "MRSA", "Ward 3B", DateTime.UtcNow.AddDays(-11), DateTime.UtcNow.AddDays(-8), 3, "Resolved"),
    ];

    public Task<List<DeviceSummary>> GetSummariesAsync()
        => Task.FromResult(Summaries);

    public Task<List<DeviceInfection>> GetInfectionsAsync(string? deviceType = null)
    {
        var result = Infections.AsEnumerable();
        if (!string.IsNullOrEmpty(deviceType))
            result = result.Where(i => i.DeviceType.Equals(deviceType, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result.ToList());
    }

    public Task<DeviceInfection> CreateInfectionAsync(CreateDeviceInfectionRequest request)
    {
        var id = $"DI{Interlocked.Increment(ref _nextId):D3}";
        var days = (request.InfectionDate - request.InsertionDate).Days;
        var infection = new DeviceInfection(
            id, request.PatientId, request.PatientName, request.DeviceType,
            request.Organism, request.Ward, request.InsertionDate,
            request.InfectionDate, days, "Active"
        );
        Infections.Add(infection);
        return Task.FromResult(infection);
    }
}
