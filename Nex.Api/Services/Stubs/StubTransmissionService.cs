using Nex.Api.Models;

namespace Nex.Api.Services.Stubs;

public class StubTransmissionService : ITransmissionService
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static readonly TransmissionNetwork MrsaNetwork = new(
        [
            new TransmissionNode("P001", "James Wilson", "ICU-A", "MRSA", Now.AddDays(-9), "Index"),
            new TransmissionNode("P008", "Emma Brown", "Ward 3B", "MRSA", Now.AddDays(-8), "Secondary"),
            new TransmissionNode("P011", "Michael O'Brien", "ICU-B", "MRSA", Now.AddDays(-6), "Secondary"),
            new TransmissionNode("ENV01", "ICU-A Bed Rail", "ICU-A", "MRSA", Now.AddDays(-7), "Environmental"),
            new TransmissionNode("HCW01", "HCW Contact A", "ICU-A", "MRSA", Now.AddDays(-7), "HCW"),
            new TransmissionNode("P099", "Unknown Source", "External", "MRSA", Now.AddDays(-12), "Suspected Source"),
        ],
        [
            new TransmissionLink("P099", "P001", "Admission", 0.6, "Patient transferred from facility with known MRSA"),
            new TransmissionLink("P001", "ENV01", "Environmental", 0.85, "Positive environmental swab at bedside"),
            new TransmissionLink("P001", "HCW01", "Direct Contact", 0.7, "Shared healthcare worker on same shift"),
            new TransmissionLink("HCW01", "P008", "Direct Contact", 0.65, "HCW provided care to both patients"),
            new TransmissionLink("P001", "P011", "Ward Transfer", 0.75, "Patient moved from ICU-A to ICU-B; temporal overlap"),
        ],
        "MRSA",
        3
    );

    private static readonly TransmissionNetwork CdiffNetwork = new(
        [
            new TransmissionNode("P005", "David Thompson", "Ward 4C", "C. difficile", Now.AddDays(-7), "Index"),
            new TransmissionNode("P012", "Yuki Tanaka", "Ward 4C", "C. difficile", Now.AddDays(-5), "Secondary"),
            new TransmissionNode("ENV02", "Ward 4C Bathroom", "Ward 4C", "C. difficile", Now.AddDays(-6), "Environmental"),
        ],
        [
            new TransmissionLink("P005", "ENV02", "Environmental", 0.9, "Spore contamination in shared bathroom"),
            new TransmissionLink("ENV02", "P012", "Environmental", 0.8, "Same bathroom used; spore persistence"),
        ],
        "C. difficile",
        2
    );

    public Task<TransmissionNetwork> GetNetworkAsync(string? organism = null)
    {
        if (organism?.Contains("diff", StringComparison.OrdinalIgnoreCase) == true)
            return Task.FromResult(CdiffNetwork);

        if (string.IsNullOrEmpty(organism) || organism.Equals("MRSA", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(MrsaNetwork);

        return Task.FromResult(new TransmissionNetwork([], [], organism ?? "Unknown", 0));
    }
}
