using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to create a CMS request filter with declarative matching rules.</summary>
public sealed record CreateRequestFilterCommand(
    string Name,
    string Key,
    Guid? SiteId = null,
    RequestFilterMode FilterMode = RequestFilterMode.AllMatch,
    IReadOnlyList<RequestFilterRuleSpec>? Rules = null,
    bool IsActive = true
) : ICommand<RequestFilterResponse>;
