using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IOutbreakService
{
    Task<List<Outbreak>> GetAllAsync(string? status = null);
    Task<OutbreakDetail?> GetByIdAsync(string id);
    Task<Outbreak> CreateAsync(CreateOutbreakRequest request);
}
