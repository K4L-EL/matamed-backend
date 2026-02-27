using Nex.Api.Models;

namespace Nex.Api.Services;

public interface IResistanceService
{
    Task<List<ResistanceSummary>> GetSummariesAsync();
    Task<List<PrescribingRecord>> GetPrescriptionsAsync(string? antibiotic = null);
}
