using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IForecastService
{
    Task<List<ForecastRiskScore>> GetRiskScoresAsync();
    Task<List<LocationRisk>> GetLocationRisksAsync();
    Task<List<ForecastTrend>> GetTrendsAsync(int days = 14);
}
