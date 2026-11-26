using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Logged outreach or workflow activity on an EngagementRequest.</summary>
public sealed class EngagementRequestActivity : Entity<EngagementRequestActivityId>
{
    private EngagementRequestActivity() { }

    internal EngagementRequestActivity(
        EngagementRequestActivityId id,
        EngagementRequestId engagementRequestId,
        Guid? activityTypeId,
        string activityName,
        Guid? connectorPersonId,
        string? note,
        DateTimeOffset occurredAtUtc
    )
        : base(id)
    {
        EngagementRequestId = engagementRequestId;
        ActivityTypeId = activityTypeId is { } g && g != Guid.Empty ? g : null;
        ActivityName = activityName.Trim();
        ConnectorPersonId = connectorPersonId is { } cp && cp != Guid.Empty ? cp : null;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public EngagementRequestId EngagementRequestId { get; private set; }
    public Guid? ActivityTypeId { get; private set; }
    public string ActivityName { get; private set; } = string.Empty;
    public Guid? ConnectorPersonId { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal bool RepointConnector(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ConnectorPersonId != retiredPersonId)
        {
            return false;
        }

        ConnectorPersonId = survivingPersonId;
        return true;
    }
}
