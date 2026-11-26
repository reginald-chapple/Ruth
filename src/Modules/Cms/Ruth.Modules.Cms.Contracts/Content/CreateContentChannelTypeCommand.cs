using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Content;

/// <summary>Command to create a CMS content channel type.</summary>
public sealed record CreateContentChannelTypeCommand(
    string Name,
    string Key,
    ContentChannelDateType DateRangeType = ContentChannelDateType.DateRange,
    bool DisablePriority = false,
    bool DisableStatus = false,
    bool IncludeTime = true
) : ICommand<ContentChannelTypeResponse>;
