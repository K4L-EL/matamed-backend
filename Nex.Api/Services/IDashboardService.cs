using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync();
    Task<DashboardTrends> GetTrendsAsync(int days = 30);
}
