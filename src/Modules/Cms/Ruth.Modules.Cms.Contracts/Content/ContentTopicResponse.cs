namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Represents a hierarchical editorial topic used to categorize CMS content items.</summary>
public sealed record ContentTopicResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentTopicId,
    int Order,
    bool IsActive
);
