using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to save a new versioned HTML/merge-token content revision for a CMS block.</summary>
public sealed record SaveHtmlContentVersionCommand(
    Guid BlockId,
    string Content,
    string? EntityValue = null,
    string? ContentSummary = null,
    DateTimeOffset? StartDateTimeUtc = null,
    DateTimeOffset? ExpireDateTimeUtc = null,
    bool AutoApprove = false,
    Guid? ApprovedByPersonId = null
) : ICommand<HtmlContentResponse>;
