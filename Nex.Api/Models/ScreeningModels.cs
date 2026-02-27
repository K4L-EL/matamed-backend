namespace Nex.Api.Models;

public record ScreeningCompliance(
    string Ward,
    int TotalRequired,
    int Completed,
    int Overdue,
    double ComplianceRate
);

public record ScreeningRecord(
    string Id,
    string PatientId,
    string PatientName,
    string Ward,
    string ScreeningType,
    string Status,
    DateTime DueDate,
    DateTime? CompletedDate,
    string? Result
);
