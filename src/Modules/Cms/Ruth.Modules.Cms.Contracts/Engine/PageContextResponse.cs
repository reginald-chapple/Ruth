namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a contextual entity binding extracted from route or query parameters.</summary>
public sealed record PageContextResponse(
    Guid Id,
    Guid PageId,
    string EntityKey,
    string IdParameter
);
