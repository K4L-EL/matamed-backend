namespace Nex.Api.Models;

public record ResistancePattern(
    string Organism,
    string Antibiotic,
    double ResistanceRate,
    int SampleCount,
    string Trend
);

public record ResistanceSummary(
    string Organism,
    int TotalIsolates,
    double MdrRate,
    List<ResistancePattern> Patterns
);

public record PrescribingRecord(
    string Id,
    string PatientId,
    string PatientName,
    string Antibiotic,
    string Indication,
    DateTime StartDate,
    DateTime? EndDate,
    int DurationDays,
    string Status,
    bool Appropriate
);
