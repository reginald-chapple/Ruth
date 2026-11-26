using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Bookmarks;

/// <summary>Command to add a bookmark link into a staff personal link section.</summary>
public sealed record AddPersonalLinkCommand(
    Guid SectionId,
    string Name,
    string Url,
    string? IconName = null,
    int Order = 0
) : ICommand<PersonalLinkSectionResponse>;
