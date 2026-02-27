using Nex.Api.Models;

namespace Nex.Api.Services;

public interface ITransmissionService
{
    Task<TransmissionNetwork> GetNetworkAsync(string? organism = null);
}
