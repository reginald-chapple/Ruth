using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Audience poll vote or Q&amp;A response submitted during a live interactive session.</summary>
public sealed class InteractiveResponse : Entity<InteractiveResponseId>
{
    private InteractiveResponse() { }

    internal InteractiveResponse(
        InteractiveResponseId id,
        InteractiveSessionId sessionId,
        InteractiveActionId actionId,
        InteractiveSessionOccurrenceId? occurrenceId,
        string participantToken,
        Guid? responderPersonId,
        string? selectedOption,
        string? textResponse,
        InteractiveModerationStatus moderationStatus,
        DateTimeOffset submittedAtUtc
    )
        : base(id)
    {
        SessionId = sessionId;
        ActionId = actionId;
        OccurrenceId = occurrenceId;
        ParticipantToken = participantToken;
        ResponderPersonId = responderPersonId is { } rp && rp != Guid.Empty ? rp : null;
        SelectedOption = selectedOption;
        TextResponse = textResponse;
        ModerationStatus = moderationStatus;
        SubmittedAtUtc = submittedAtUtc.ToUniversalTime();
    }

    public InteractiveSessionId SessionId { get; private set; }
    public InteractiveActionId ActionId { get; private set; }
    public InteractiveSessionOccurrenceId? OccurrenceId { get; private set; }
    public string ParticipantToken { get; private set; } = string.Empty;
    public Guid? ResponderPersonId { get; private set; }
    public string? SelectedOption { get; private set; }
    public string? TextResponse { get; private set; }
    public InteractiveModerationStatus ModerationStatus { get; private set; }
    public int Upvotes { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    internal void Moderate(InteractiveModerationStatus status, bool incrementUpvote)
    {
        ModerationStatus = status;
        if (incrementUpvote)
        {
            Upvotes++;
        }
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ResponderPersonId != retiredPersonId)
        {
            return false;
        }

        ResponderPersonId = survivingPersonId;
        return true;
    }
}
