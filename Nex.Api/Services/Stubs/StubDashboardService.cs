using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubDashboardService : IDashboardService
{
    public Task<DashboardSummary> GetSummaryAsync()
    {
        var summary = new DashboardSummary(
            ActiveInfections: StubData.Infections.Count(i => i.Status == "Active"),
            PatientsAtRisk: StubData.Patients.Count(p => p.RiskScore > 0.5),
            ActiveOutbreaks: StubData.Outbreaks.Count(o => o.Status != "Resolved"),
            PendingAlerts: StubData.Alerts.Count(a => !a.IsRead),
            InfectionRateChange: -12.5,
            RiskScoreAverage: Math.Round(StubData.Patients.Average(p => p.RiskScore), 2)
        );
        return Task.FromResult(summary);
    }

    public Task<DashboardTrends> GetTrendsAsync(int days = 30)
    {
        var trends = new DashboardTrends(
            StubData.GenerateInfectionTrends(days),
            StubData.GenerateInfectionTrends(days)
        );
        return Task.FromResult(trends);
    }
}
