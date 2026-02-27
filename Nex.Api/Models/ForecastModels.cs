namespace Nex.Api.Models;

public record ForecastRiskScore(
    string PatientId,
    string PatientName,
    string Ward,
    double Score,
    string RiskLevel,
    List<string> TopFactors
);

public record LocationRisk(
    string LocationId,
    string Name,
    string Type,
    double RiskScore,
    int ActiveCases,
    int Capacity,
    double OccupancyRate
);

public record ForecastTrend(
    DateTime Date,
    double PredictedCount,
    double LowerBound,
    double UpperBound,
    double ActualCount
);
