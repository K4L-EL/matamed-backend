using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubForecastService : IForecastService
{
    public Task<List<ForecastRiskScore>> GetRiskScoresAsync()
    {
        var scores = StubData.Patients
            .Where(p => p.RiskScore > 0.3)
            .OrderByDescending(p => p.RiskScore)
            .Select(p => new ForecastRiskScore(
                p.Id,
                p.Name,
                p.Ward,
                p.RiskScore,
                p.RiskScore > 0.75 ? "Critical" : p.RiskScore > 0.5 ? "High" : "Medium",
                p.Organisms.Count > 0
                    ? p.Organisms.Take(2).ToList()
                    : ["Prolonged stay", "Invasive devices"]
            ))
            .ToList();

        return Task.FromResult(scores);
    }

    public Task<List<LocationRisk>> GetLocationRisksAsync()
    {
        return Task.FromResult(StubData.LocationRisks);
    }

    public Task<List<ForecastTrend>> GetTrendsAsync(int days = 14)
    {
        return Task.FromResult(StubData.GenerateForecastTrends(days));
    }
}
