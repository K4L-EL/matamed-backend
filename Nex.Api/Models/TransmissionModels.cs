namespace Nex.Api.Models;

public record TransmissionNode(
    string Id,
    string PatientName,
    string Ward,
    string Organism,
    DateTime DetectedAt,
    string NodeType
);

public record TransmissionLink(
    string SourceId,
    string TargetId,
    string LinkType,
    double Confidence,
    string Evidence
);

public record TransmissionNetwork(
    List<TransmissionNode> Nodes,
    List<TransmissionLink> Links,
    string Organism,
    int TotalCases
);
