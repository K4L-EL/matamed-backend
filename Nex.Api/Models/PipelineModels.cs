namespace Nex.Api.Models;

public record Pipeline(
    string Id,
    string Name,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? LastRunAt,
    List<PipelineNode> Nodes,
    List<PipelineEdge> Edges
);

public record PipelineNode(
    string Id,
    string Type,
    string Label,
    double PositionX,
    double PositionY,
    Dictionary<string, string> Config
);

public record PipelineEdge(
    string Id,
    string SourceId,
    string TargetId,
    string? Label
);

public record CreatePipelineRequest(
    string Name,
    string Description,
    List<PipelineNode> Nodes,
    List<PipelineEdge> Edges
);
