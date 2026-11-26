namespace Ruth.Modules.Cms.Contracts.Bookmarks;

/// <summary>Represents a personal or shared staff bookmarks section.</summary>
public sealed record PersonalLinkSectionResponse(
    Guid Id,
    string Name,
    Guid? PersonId,
    bool IsShared,
    int Order,
    IReadOnlyList<PersonalLinkResponse> Links
);
