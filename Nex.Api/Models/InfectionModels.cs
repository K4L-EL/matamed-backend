namespace Nex.Api.Models;

public record Infection(
    string Id,
    string PatientId,
    string PatientName,
    string Organism,
    string Type,
    string Location,
    string Ward,
    string Status,
    DateTime DetectedAt,
    DateTime? ResolvedAt,
    string Severity,
    bool IsHai
);

public record InfectionDetail(
    Infection Infection,
    List<string> RiskFactors,
    List<InfectionEvent> Timeline
);

public record InfectionEvent(
    DateTime Timestamp,
    string Description,
    string EventType
);
