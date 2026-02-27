using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IInfectionService
{
    Task<List<Infection>> GetAllAsync(string? status = null, string? ward = null);
    Task<InfectionDetail?> GetByIdAsync(string id);
    Task<Infection> CreateAsync(CreateInfectionRequest request);
}
