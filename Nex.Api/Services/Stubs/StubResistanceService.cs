using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubResistanceService : IResistanceService
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static readonly List<ResistanceSummary> Summaries =
    [
        new("MRSA", 45, 0.62, [
            new ResistancePattern("MRSA", "Oxacillin", 1.0, 45, "Stable"),
            new ResistancePattern("MRSA", "Vancomycin", 0.02, 45, "Stable"),
            new ResistancePattern("MRSA", "Linezolid", 0.04, 45, "Stable"),
            new ResistancePattern("MRSA", "Clindamycin", 0.38, 45, "Increasing"),
            new ResistancePattern("MRSA", "Trimethoprim", 0.22, 45, "Stable"),
        ]),
        new("E. coli", 82, 0.31, [
            new ResistancePattern("E. coli", "Amoxicillin", 0.65, 82, "Stable"),
            new ResistancePattern("E. coli", "Ciprofloxacin", 0.28, 82, "Increasing"),
            new ResistancePattern("E. coli", "Ceftriaxone", 0.15, 82, "Stable"),
            new ResistancePattern("E. coli", "Meropenem", 0.02, 82, "Stable"),
            new ResistancePattern("E. coli", "Gentamicin", 0.12, 82, "Decreasing"),
        ]),
        new("Klebsiella pneumoniae", 28, 0.46, [
            new ResistancePattern("Klebsiella pneumoniae", "Ceftriaxone", 0.46, 28, "Increasing"),
            new ResistancePattern("Klebsiella pneumoniae", "Meropenem", 0.11, 28, "Increasing"),
            new ResistancePattern("Klebsiella pneumoniae", "Ciprofloxacin", 0.32, 28, "Stable"),
            new ResistancePattern("Klebsiella pneumoniae", "Amikacin", 0.07, 28, "Stable"),
        ]),
        new("Pseudomonas aeruginosa", 19, 0.37, [
            new ResistancePattern("Pseudomonas aeruginosa", "Piperacillin-tazobactam", 0.21, 19, "Stable"),
            new ResistancePattern("Pseudomonas aeruginosa", "Meropenem", 0.16, 19, "Increasing"),
            new ResistancePattern("Pseudomonas aeruginosa", "Ciprofloxacin", 0.37, 19, "Stable"),
            new ResistancePattern("Pseudomonas aeruginosa", "Ceftazidime", 0.26, 19, "Stable"),
        ])
    ];

    private static readonly List<PrescribingRecord> Prescriptions =
    [
        new("RX001", "P001", "James Wilson", "Vancomycin", "MRSA BSI", Now.AddDays(-5), null, 5, "Active", true),
        new("RX002", "P001", "James Wilson", "Metronidazole", "C. difficile", Now.AddDays(-3), null, 3, "Active", true),
        new("RX003", "P002", "Sarah Chen", "Ceftriaxone", "UTI", Now.AddDays(-2), null, 2, "Active", true),
        new("RX004", "P003", "Ahmed Hassan", "Meropenem", "VAP", Now.AddDays(-8), null, 8, "Active", true),
        new("RX005", "P005", "David Thompson", "Linezolid", "VRE wound", Now.AddDays(-6), null, 6, "Active", true),
        new("RX006", "P007", "Robert Kim", "Piperacillin-tazobactam", "HAP", Now.AddDays(-4), null, 4, "Active", false),
        new("RX007", "P008", "Emma Brown", "Flucloxacillin", "SSTI", Now.AddDays(-7), Now.AddDays(-2), 5, "Completed", true),
        new("RX008", "P011", "Michael O'Brien", "Caspofungin", "Candidaemia", Now.AddDays(-3), null, 3, "Active", true),
        new("RX009", "P004", "Maria Garcia", "Amoxicillin", "Prophylaxis", Now.AddDays(-4), Now.AddDays(-1), 3, "Completed", false),
    ];

    public Task<List<ResistanceSummary>> GetSummariesAsync()
        => Task.FromResult(Summaries);

    public Task<List<PrescribingRecord>> GetPrescriptionsAsync(string? antibiotic = null)
    {
        var result = Prescriptions.AsEnumerable();
        if (!string.IsNullOrEmpty(antibiotic))
            result = result.Where(r => r.Antibiotic.Equals(antibiotic, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result.ToList());
    }
}
