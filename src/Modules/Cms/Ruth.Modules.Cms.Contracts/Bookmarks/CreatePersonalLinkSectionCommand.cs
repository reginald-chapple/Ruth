using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Bookmarks;

/// <summary>Command to create a personal or shared staff bookmark section.</summary>
public sealed record CreatePersonalLinkSectionCommand(
    string Name,
    Guid? PersonId = null,
    bool IsShared = false,
    int Order = 0
) : ICommand<PersonalLinkSectionResponse>;
