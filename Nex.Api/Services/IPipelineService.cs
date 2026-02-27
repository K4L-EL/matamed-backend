using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IPipelineService
{
    Task<List<Pipeline>> GetAllAsync();
    Task<Pipeline?> GetByIdAsync(string id);
    Task<Pipeline> CreateAsync(CreatePipelineRequest request);
    Task<Pipeline?> UpdateAsync(string id, CreatePipelineRequest request);
}
