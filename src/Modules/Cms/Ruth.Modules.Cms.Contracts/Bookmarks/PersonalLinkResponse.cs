namespace Ruth.Modules.Cms.Contracts.Bookmarks;

/// <summary>Represents a single bookmark entry inside a staff personal link section.</summary>
public sealed record PersonalLinkResponse(
    Guid Id,
    Guid SectionId,
    string Name,
    string Url,
    string? IconName,
    int Order
);
