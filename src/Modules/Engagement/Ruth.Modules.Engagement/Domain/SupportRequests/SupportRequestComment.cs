using Ruth.SharedKernel.Primitives;
using static Ruth.Modules.Engagement.Domain.EngagementTextNormalizer;

namespace Ruth.Modules.Engagement.Domain.SupportRequests;

/// <summary>Public comment or internal care-team note attached to a SupportRequest.</summary>
public sealed class SupportRequestComment : Entity<SupportRequestCommentId>
{
    private SupportRequestComment() { }

    internal SupportRequestComment(
        SupportRequestCommentId id,
        SupportRequestId supportRequestId,
        Guid? authorPersonId,
        string authorDisplayName,
        string body,
        bool isInternalCareNote,
        DateTimeOffset createdAtUtc
    )
        : base(id)
    {
        SupportRequestId = supportRequestId;
        AuthorPersonId = NormalizeGuid(authorPersonId);
        AuthorDisplayName = authorDisplayName.Trim();
        Body = body.Trim();
        IsInternalCareNote = isInternalCareNote;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public SupportRequestId SupportRequestId { get; private set; }
    public Guid? AuthorPersonId { get; private set; }
    public string AuthorDisplayName { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public bool IsInternalCareNote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal bool RepointAuthor(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (AuthorPersonId != retiredPersonId)
        {
            return false;
        }

        AuthorPersonId = survivingPersonId;
        return true;
    }
}
