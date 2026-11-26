namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Represents a versioned and approvable HTML/merge-token content revision for a CMS block.</summary>
public sealed record HtmlContentResponse(
    Guid Id,
    Guid BlockId,
    string? EntityValue,
    int Version,
    string Content,
    string? ContentSummary,
    DateTimeOffset? StartDateTimeUtc,
    DateTimeOffset? ExpireDateTimeUtc,
    bool IsApproved,
    Guid? ApprovedByPersonId,
    DateTimeOffset? ApprovedDateTimeUtc,
    DateTimeOffset CreatedAtUtc
);
