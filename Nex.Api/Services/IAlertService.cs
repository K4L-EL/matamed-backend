using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IAlertService
{
    Task<List<Alert>> GetAllAsync(bool? unreadOnly = null);
    Task<Alert?> MarkAsReadAsync(string id);
    Task<Alert> CreateAsync(CreateAlertRequest request);
}
