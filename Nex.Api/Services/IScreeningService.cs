using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IScreeningService
{
    Task<List<ScreeningCompliance>> GetComplianceAsync();
    Task<List<ScreeningRecord>> GetRecordsAsync(string? ward = null, string? status = null);
    Task<ScreeningRecord> CreateRecordAsync(CreateScreeningRecordRequest request);
}
