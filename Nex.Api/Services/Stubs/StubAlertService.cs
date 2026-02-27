using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubAlertService : IAlertService
{
    private static int _nextId = StubData.Alerts.Count + 1;

    public Task<List<Alert>> GetAllAsync(bool? unreadOnly = null)
    {
        var alerts = StubData.Alerts.AsEnumerable();

        if (unreadOnly == true)
            alerts = alerts.Where(a => !a.IsRead);

        return Task.FromResult(alerts.OrderByDescending(a => a.CreatedAt).ToList());
    }

    public Task<Alert?> MarkAsReadAsync(string id)
    {
        var idx = StubData.Alerts.FindIndex(a => a.Id == id);
        if (idx < 0) return Task.FromResult<Alert?>(null);
        StubData.Alerts[idx] = StubData.Alerts[idx] with { IsRead = true };
        return Task.FromResult<Alert?>(StubData.Alerts[idx]);
    }

    public Task<Alert> CreateAsync(CreateAlertRequest request)
    {
        var id = $"ALT{Interlocked.Increment(ref _nextId):D3}";
        var alert = new Alert(
            id, request.Title, request.Description, request.Severity,
            request.Category, DateTime.UtcNow, false, null, null
        );
        StubData.Alerts.Add(alert);
        return Task.FromResult(alert);
    }
}
