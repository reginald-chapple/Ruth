using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Live run occurrence of an <see cref="InteractiveSession" />.</summary>
public sealed class InteractiveSessionOccurrence : Entity<InteractiveSessionOccurrenceId>
{
    private InteractiveSessionOccurrence() { }

    internal InteractiveSessionOccurrence(
        InteractiveSessionOccurrenceId id,
        InteractiveSessionId sessionId,
        Guid? eventItemOccurrenceId,
        Guid? branchId,
        DateTimeOffset startedAtUtc
    )
        : base(id)
    {
        SessionId = sessionId;
        EventItemOccurrenceId = eventItemOccurrenceId is { } eo && eo != Guid.Empty ? eo : null;
        BranchId = branchId is { } br && br != Guid.Empty ? br : null;
        StartedAtUtc = startedAtUtc.ToUniversalTime();
    }

    public InteractiveSessionId SessionId { get; private set; }
    public Guid? EventItemOccurrenceId { get; private set; }
    public Guid? BranchId { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    internal void End(DateTimeOffset endedAtUtc)
    {
        EndedAtUtc ??= endedAtUtc.ToUniversalTime();
    }
}
