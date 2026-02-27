namespace Nex.Api.Models;

public record DashboardSummary(
    int ActiveInfections,
    int PatientsAtRisk,
    int ActiveOutbreaks,
    int PendingAlerts,
    double InfectionRateChange,
    double RiskScoreAverage
);

public record TrendPoint(
    DateTime Date,
    int Count,
    string Category
);

public record DashboardTrends(
    List<TrendPoint> InfectionTrends,
    List<TrendPoint> AdmissionTrends
);
