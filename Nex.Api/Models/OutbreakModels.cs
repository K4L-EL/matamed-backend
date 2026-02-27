namespace Nex.Api.Models;

public record Outbreak(
    string Id,
    string Organism,
    string Location,
    DateTime DetectedAt,
    DateTime? ResolvedAt,
    string Status,
    int AffectedPatients,
    string Severity,
    string InvestigationStatus
);

public record OutbreakDetail(
    Outbreak Outbreak,
    List<string> AffectedWards,
    List<OutbreakTimeline> Timeline,
    List<string> ScreeningGuidance
);

public record OutbreakTimeline(
    DateTime Timestamp,
    string Event,
    string Description
);
