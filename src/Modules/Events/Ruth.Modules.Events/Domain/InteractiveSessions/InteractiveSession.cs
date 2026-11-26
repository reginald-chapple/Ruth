using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Real-time town-hall, conference, or classroom interactive session aggregate.</summary>
public sealed class InteractiveSession
    : AggregateRoot<InteractiveSessionId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Events.InteractiveSession";
    private const string JoinCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly List<InteractiveAction> _actions = [];
    private readonly List<InteractiveSessionOccurrence> _occurrences = [];
    private readonly List<InteractiveResponse> _responses = [];

    private InteractiveSession() { }

    private InteractiveSession(InteractiveSessionId id, string joinCode)
        : base(id)
    {
        JoinCode = joinCode;
    }

    public string Title { get; private set; } = string.Empty;
    public string JoinCode { get; private set; } = string.Empty;
    public Guid? EventItemOccurrenceId { get; private set; }
    public Guid? ModeratorPersonId { get; private set; }
    public InteractiveSessionStatus Status { get; private set; } = InteractiveSessionStatus.Draft;
    public InteractiveActionId? ActiveActionId { get; private set; }
    public InteractiveSessionOccurrenceId? ActiveOccurrenceId { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }

    public IReadOnlyCollection<InteractiveAction> Actions => _actions.AsReadOnly();
    public IReadOnlyCollection<InteractiveSessionOccurrence> Occurrences =>
        _occurrences.AsReadOnly();
    public IReadOnlyCollection<InteractiveResponse> Responses => _responses.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

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

    public static Result<InteractiveSession> Create(
        string title,
        Guid? eventItemOccurrenceId = null,
        Guid? moderatorPersonId = null,
        IReadOnlyList<InteractiveActionSpec>? initialActions = null
    )
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
        {
            return EventsErrors.InteractiveSessionInvalid;
        }

        var id = InteractiveSessionId.Create();
        var session = new InteractiveSession(id, GenerateJoinCode(id.Value))
        {
            Title = title.Trim(),
            EventItemOccurrenceId = NormalizeGuid(eventItemOccurrenceId),
            ModeratorPersonId = NormalizeGuid(moderatorPersonId),
        };

        var initRes = session.AddInitialActions(initialActions);
        return initRes.IsFailure ? Result.Failure<InteractiveSession>(initRes.Errors) : session;
    }

    private Result AddInitialActions(IReadOnlyList<InteractiveActionSpec>? initialActions)
    {
        if (initialActions is null)
        {
            return Result.Success();
        }

        foreach (var actionSpec in initialActions)
        {
            var added = AddAction(actionSpec);
            if (added.IsFailure)
            {
                return added;
            }
        }

        return Result.Success();
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    public void RegenerateJoinCode()
    {
        JoinCode = GenerateJoinCode(Guid.CreateVersion7());
    }

    public Result<InteractiveAction> AddAction(InteractiveActionSpec spec)
    {
        var created = InteractiveAction.Create(Id, spec);
        if (created.IsFailure)
        {
            return created;
        }

        _actions.Add(created.Value);
        ActiveActionId ??= created.Value.Id;
        return created;
    }

    public Result<InteractiveSessionOccurrence> StartOccurrence(
        Guid? eventItemOccurrenceId,
        Guid? branchId,
        DateTimeOffset nowUtc
    )
    {
        foreach (var open in _occurrences.Where(static o => o.EndedAtUtc is null))
        {
            open.End(nowUtc);
        }

        var occurrence = new InteractiveSessionOccurrence(
            InteractiveSessionOccurrenceId.Create(),
            Id,
            eventItemOccurrenceId ?? EventItemOccurrenceId,
            branchId,
            nowUtc
        );
        _occurrences.Add(occurrence);
        ActiveOccurrenceId = occurrence.Id;
        Status = InteractiveSessionStatus.Live;
        StartedAtUtc ??= nowUtc.ToUniversalTime();
        EndedAtUtc = null;
        return occurrence;
    }

    public Result EndSession(DateTimeOffset nowUtc)
    {
        foreach (var open in _occurrences.Where(static o => o.EndedAtUtc is null))
        {
            open.End(nowUtc);
        }

        ActiveOccurrenceId = null;
        Status = InteractiveSessionStatus.Ended;
        EndedAtUtc = nowUtc.ToUniversalTime();
        return Result.Success();
    }

    public Result SetActiveAction(InteractiveActionId? actionId)
    {
        if (actionId is { } target && !target.IsEmpty && !_actions.Exists(a => a.Id == target))
        {
            return EventsErrors.InteractiveActionNotFound;
        }

        ActiveActionId = actionId is { IsEmpty: false } valid ? valid : null;
        return Result.Success();
    }

    public Result<InteractiveResponse> SubmitResponse(
        InteractiveActionId actionId,
        string participantToken,
        string? selectedOption,
        string? textResponse,
        Guid? responderPersonId,
        DateTimeOffset submittedAtUtc
    )
    {
        if (Status != InteractiveSessionStatus.Live)
        {
            return EventsErrors.InteractiveSessionNotLive;
        }

        var action = _actions.Find(a => a.Id == actionId && a.IsOpen);
        if (action is null)
        {
            return EventsErrors.InteractiveActionNotFound;
        }

        if (string.IsNullOrWhiteSpace(participantToken))
        {
            return EventsErrors.InteractiveResponseInvalid;
        }

        var normalizedToken = participantToken.Trim();
        if (HasDuplicateResponse(action, normalizedToken, responderPersonId))
        {
            return EventsErrors.InteractiveDuplicateResponse;
        }

        var validated = ValidateResponsePayload(
            action,
            selectedOption,
            textResponse,
            out var canonicalOption,
            out var cleanedText
        );
        if (validated.IsFailure)
        {
            return Result.Failure<InteractiveResponse>(validated.Errors);
        }

        var initialStatus = action.RequireModeration
            ? InteractiveModerationStatus.Pending
            : InteractiveModerationStatus.Approved;
        var response = new InteractiveResponse(
            InteractiveResponseId.Create(),
            Id,
            action.Id,
            ActiveOccurrenceId,
            normalizedToken,
            responderPersonId,
            canonicalOption,
            cleanedText,
            initialStatus,
            submittedAtUtc
        );
        _responses.Add(response);
        return response;
    }

    public Result<InteractiveResponse> ModerateResponse(
        InteractiveResponseId responseId,
        InteractiveModerationStatus status,
        bool incrementUpvote
    )
    {
        var response = _responses.Find(r => r.Id == responseId);
        if (response is null)
        {
            return EventsErrors.InteractiveResponseNotFound;
        }

        response.Moderate(status, incrementUpvote);
        return response;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (ModeratorPersonId == retiredPersonId)
        {
            ModeratorPersonId = survivingPersonId;
            changed = true;
        }

        foreach (var response in _responses)
        {
            changed |= response.RepointPerson(retiredPersonId, survivingPersonId);
        }

        return changed;
    }

    private bool HasDuplicateResponse(
        InteractiveAction action,
        string normalizedToken,
        Guid? responderPersonId
    )
    {
        return !action.AllowMultipleResponses
            && _responses.Exists(r =>
                r.ActionId == action.Id
                && r.OccurrenceId == ActiveOccurrenceId
                && MatchesResponder(r, normalizedToken, responderPersonId)
            );
    }

    private static bool MatchesResponder(
        InteractiveResponse response,
        string normalizedToken,
        Guid? responderPersonId
    )
    {
        return string.Equals(
                response.ParticipantToken,
                normalizedToken,
                StringComparison.OrdinalIgnoreCase
            )
            || (
                responderPersonId.HasValue
                && responderPersonId.Value != Guid.Empty
                && response.ResponderPersonId == responderPersonId.Value
            );
    }

    private static Result ValidateResponsePayload(
        InteractiveAction action,
        string? selectedOption,
        string? textResponse,
        out string? canonicalOption,
        out string? cleanedText
    )
    {
        canonicalOption = null;
        cleanedText = string.IsNullOrWhiteSpace(textResponse) ? null : textResponse.Trim();

        if (action.ActionType == InteractiveActionType.MultipleChoicePoll)
        {
            canonicalOption = action.MatchCanonicalOption(selectedOption);
            return canonicalOption is null
                ? EventsErrors.InteractiveResponseInvalid
                : Result.Success();
        }

        return cleanedText is null || cleanedText.Length > 1000
            ? EventsErrors.InteractiveResponseInvalid
            : Result.Success();
    }

    private static string GenerateJoinCode(Guid seed)
    {
        Span<byte> bytes = stackalloc byte[16];
        _ = seed.TryWriteBytes(bytes);
        Span<char> chars = stackalloc char[6];
        for (var i = 0; i < 6; i++)
        {
            var randomIndex = bytes[10 + i] % JoinCodeAlphabet.Length;
            chars[i] = JoinCodeAlphabet[randomIndex];
        }

        return new string(chars);
    }
}
