using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Versioned, approvable HTML / merge-token content revision attached to a <see cref="CmsBlock" />.</summary>
public sealed class HtmlContent
    : AggregateRoot<HtmlContentId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private HtmlContent() { }

    private HtmlContent(HtmlContentId id)
        : base(id) { }

    public CmsBlockId BlockId { get; private set; }
    public string? EntityValue { get; private set; }
    public int Version { get; private set; } = 1;
    public string Content { get; private set; } = string.Empty;
    public string? ContentSummary { get; private set; }
    public DateTimeOffset? StartDateTimeUtc { get; private set; }
    public DateTimeOffset? ExpireDateTimeUtc { get; private set; }
    public bool IsApproved { get; private set; }
    public Guid? ApprovedByPersonId { get; private set; }
    public DateTimeOffset? ApprovedDateTimeUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<HtmlContent> Create(
        CmsBlockId blockId,
        int version,
        string content,
        string? entityValue = null,
        string? contentSummary = null,
        DateTimeOffset? startDateTimeUtc = null,
        DateTimeOffset? expireDateTimeUtc = null,
        bool isApproved = false,
        Guid? approvedByPersonId = null,
        DateTimeOffset? approvedAtUtc = null
    )
    {
        return blockId.IsEmpty || version < 1
                ? Result.Failure<HtmlContent>(
                    CmsErrors.Validation(
                        "HtmlContent.Invalid",
                        "BlockId and positive Version are required."
                    )
                )
            : startDateTimeUtc is not null
            && expireDateTimeUtc is not null
            && expireDateTimeUtc <= startDateTimeUtc
                ? Result.Failure<HtmlContent>(
                    CmsErrors.Validation(
                        "HtmlContent.WindowInvalid",
                        "Expiration date must be after start date."
                    )
                )
            : Result.Success(
                new HtmlContent(HtmlContentId.Create())
                {
                    BlockId = blockId,
                    Version = version,
                    Content = content ?? string.Empty,
                    EntityValue = string.IsNullOrWhiteSpace(entityValue)
                        ? null
                        : entityValue.Trim(),
                    ContentSummary = string.IsNullOrWhiteSpace(contentSummary)
                        ? null
                        : contentSummary.Trim(),
                    StartDateTimeUtc = startDateTimeUtc?.ToUniversalTime(),
                    ExpireDateTimeUtc = expireDateTimeUtc?.ToUniversalTime(),
                    IsApproved = isApproved,
                    ApprovedByPersonId = isApproved ? approvedByPersonId : null,
                    ApprovedDateTimeUtc = isApproved ? approvedAtUtc?.ToUniversalTime() : null,
                }
            );
    }

    public void SetApproval(bool isApproved, Guid? approvedByPersonId, DateTimeOffset nowUtc)
    {
        IsApproved = isApproved;
        if (isApproved)
        {
            ApprovedByPersonId = approvedByPersonId;
            ApprovedDateTimeUtc = nowUtc.ToUniversalTime();
        }
        else
        {
            ApprovedByPersonId = null;
            ApprovedDateTimeUtc = null;
        }
    }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return IsApproved
            && (StartDateTimeUtc is null || !(StartDateTimeUtc > nowUtc))
            && (ExpireDateTimeUtc is null || ExpireDateTimeUtc > nowUtc);
    }

    public bool RepointApprovedByPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ApprovedByPersonId != retiredPersonId)
        {
            return false;
        }

        ApprovedByPersonId = survivingPersonId;
        return true;
    }
}
