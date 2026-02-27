namespace Nex.Api.Models;

public record Patient(
    string Id,
    string Name,
    int Age,
    string Gender,
    string Ward,
    string BedNumber,
    DateTime AdmittedAt,
    string Status,
    double RiskScore,
    int ActiveInfections,
    List<string> Organisms
);

public record PatientRisk(
    string PatientId,
    double OverallScore,
    List<RiskFactor> Factors,
    string RiskLevel
);

public record RiskFactor(
    string Name,
    double Contribution,
    string Description
);
