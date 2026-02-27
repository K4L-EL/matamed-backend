namespace Nex.Api.Models;

public record Alert(
    string Id,
    string Title,
    string Description,
    string Severity,
    string Category,
    DateTime CreatedAt,
    bool IsRead,
    string? RelatedEntityId,
    string? RelatedEntityType
);
