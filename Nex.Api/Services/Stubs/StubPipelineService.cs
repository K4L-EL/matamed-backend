using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubPipelineService : IPipelineService
{
    private static int _nextId = 4;
    private static readonly DateTime Now = DateTime.UtcNow;

    private static readonly List<Pipeline> Pipelines =
    [
        new("PL001", "MRSA Outbreak Detection", "Monitors patient data for MRSA clusters and triggers outbreak alerts", "Active", Now.AddDays(-14), Now.AddHours(-2),
            [
                new("n1", "source", "Patient Registry", 0, 100, new() { { "entity", "patients" } }),
                new("n2", "source", "Infection Data", 0, 300, new() { { "entity", "infections" } }),
                new("n3", "transform", "Filter MRSA", 300, 100, new() { { "organism", "MRSA" }, { "status", "Active" } }),
                new("n4", "transform", "Cluster Detection", 300, 300, new() { { "timeWindow", "72h" }, { "minCases", "2" } }),
                new("n5", "transform", "Risk Score", 600, 200, new() { { "model", "logistic_v2" } }),
                new("n6", "output", "Outbreak Alert", 900, 100, new() { { "severity", "Critical" }, { "channel", "alerts" } }),
                new("n7", "output", "Dashboard Widget", 900, 300, new() { { "target", "dashboard" } }),
            ],
            [
                new("e1", "n1", "n3", "patients"),
                new("e2", "n2", "n3", "infections"),
                new("e3", "n2", "n4", null),
                new("e4", "n3", "n5", "filtered"),
                new("e5", "n4", "n5", "clusters"),
                new("e6", "n5", "n6", "high risk"),
                new("e7", "n5", "n7", "all"),
            ]),
        new("PL002", "HAI Compliance Monitor", "Tracks screening compliance and generates alerts for overdue screenings", "Active", Now.AddDays(-10), Now.AddHours(-6),
            [
                new("n1", "source", "Screening Records", 0, 150, new() { { "entity", "screening" } }),
                new("n2", "source", "Patient Registry", 0, 350, new() { { "entity", "patients" } }),
                new("n3", "transform", "Overdue Filter", 300, 150, new() { { "status", "Overdue" } }),
                new("n4", "transform", "Compliance Rate", 300, 350, new() { { "groupBy", "ward" } }),
                new("n5", "output", "Compliance Alert", 600, 150, new() { { "threshold", "80%" } }),
                new("n6", "output", "Ward Report", 600, 350, new() { { "format", "table" } }),
            ],
            [
                new("e1", "n1", "n3", null),
                new("e2", "n1", "n4", null),
                new("e3", "n2", "n4", null),
                new("e4", "n3", "n5", "overdue"),
                new("e5", "n4", "n6", "rates"),
            ]),
        new("PL003", "Device Infection Tracker", "Correlates device usage with infection events for prevention insights", "Paused", Now.AddDays(-7), Now.AddDays(-2),
            [
                new("n1", "source", "Device Logs", 0, 200, new() { { "entity", "devices" } }),
                new("n2", "transform", "Duration Calc", 300, 200, new() { { "metric", "daysToInfection" } }),
                new("n3", "output", "Risk Report", 600, 200, new() { { "format", "chart" } }),
            ],
            [
                new("e1", "n1", "n2", null),
                new("e2", "n2", "n3", null),
            ]),
    ];

    public Task<List<Pipeline>> GetAllAsync()
        => Task.FromResult(Pipelines.ToList());

    public Task<Pipeline?> GetByIdAsync(string id)
        => Task.FromResult(Pipelines.FirstOrDefault(p => p.Id == id));

    public Task<Pipeline> CreateAsync(CreatePipelineRequest request)
    {
        var id = $"PL{Interlocked.Increment(ref _nextId):D3}";
        var pipeline = new Pipeline(id, request.Name, request.Description, "Draft", DateTime.UtcNow, null, request.Nodes, request.Edges);
        Pipelines.Add(pipeline);
        return Task.FromResult(pipeline);
    }

    public Task<Pipeline?> UpdateAsync(string id, CreatePipelineRequest request)
    {
        var idx = Pipelines.FindIndex(p => p.Id == id);
        if (idx < 0) return Task.FromResult<Pipeline?>(null);
        Pipelines[idx] = Pipelines[idx] with { Name = request.Name, Description = request.Description, Nodes = request.Nodes, Edges = request.Edges };
        return Task.FromResult<Pipeline?>(Pipelines[idx]);
    }
}
