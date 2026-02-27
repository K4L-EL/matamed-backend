using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IDeviceService
{
    Task<List<DeviceSummary>> GetSummariesAsync();
    Task<List<DeviceInfection>> GetInfectionsAsync(string? deviceType = null);
    Task<DeviceInfection> CreateInfectionAsync(CreateDeviceInfectionRequest request);
}
