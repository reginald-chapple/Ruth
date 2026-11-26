using Ruth.Modules.Events.Contracts.InteractiveSessions;

namespace Ruth.Modules.Events.Domain.InteractiveSessions;

/// <summary>Computes real-time stage projections (poll tallies, percentages, and moderated Q&amp;A) for an <see cref="InteractiveSession" />.</summary>
public static class InteractiveStageProjectionCalculator
{
    public static InteractiveStageProjectionResponse BuildStageProjection(
        InteractiveSession session
    )
    {
        ArgumentNullException.ThrowIfNull(session);

        var activeAction = ResolveActiveAction(session);
        if (activeAction is null)
        {
            return BuildEmptyProjection(session);
        }

        var scopedResponses = FilterScopedResponses(session, activeAction.Id);
        var pollTallies = activeAction.ShowLiveResultsOnStage
            ? ComputePollTallies(activeAction, scopedResponses)
            : [];
        var approvedMessages = activeAction.ShowLiveResultsOnStage
            ? ProjectApprovedMessages(scopedResponses)
            : [];

        return new InteractiveStageProjectionResponse(
            session.Id.Value,
            session.Title,
            session.JoinCode,
            session.Status,
            session.ActiveOccurrenceId?.Value,
            MapAction(activeAction),
            scopedResponses.Count,
            pollTallies,
            approvedMessages
        );
    }

    private static InteractiveAction? ResolveActiveAction(InteractiveSession session)
    {
        return session.ActiveActionId is { } activeId
            ? session.Actions.FirstOrDefault(a => a.Id == activeId)
            : null;
    }

    private static InteractiveStageProjectionResponse BuildEmptyProjection(
        InteractiveSession session
    )
    {
        return new InteractiveStageProjectionResponse(
            session.Id.Value,
            session.Title,
            session.JoinCode,
            session.Status,
            session.ActiveOccurrenceId?.Value,
            ActiveAction: null,
            TotalResponsesForActiveAction: 0,
            PollTallies: [],
            ApprovedAudienceMessages: []
        );
    }

    private static List<InteractiveResponse> FilterScopedResponses(
        InteractiveSession session,
        InteractiveActionId actionId
    )
    {
        return
        [
            .. session.Responses.Where(r =>
                r.ActionId == actionId
                && (
                    session.ActiveOccurrenceId is null
                    || r.OccurrenceId == session.ActiveOccurrenceId
                )
            ),
        ];
    }

    private static InteractiveResponseDto[] ProjectApprovedMessages(
        List<InteractiveResponse> responses
    )
    {
        return
        [
            .. responses
                .Where(static r => r.ModerationStatus == InteractiveModerationStatus.Approved)
                .OrderByDescending(static r => r.Upvotes)
                .ThenBy(static r => r.SubmittedAtUtc)
                .Select(MapResponse),
        ];
    }

    public static InteractiveActionResponse MapAction(InteractiveAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        return new InteractiveActionResponse(
            action.Id.Value,
            action.SessionId.Value,
            action.Title,
            action.PromptText,
            action.ActionType,
            action.Options,
            action.DisplayOrder,
            action.AllowMultipleResponses,
            action.RequireModeration,
            action.ShowLiveResultsOnStage,
            action.IsOpen
        );
    }

    public static InteractiveResponseDto MapResponse(InteractiveResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new InteractiveResponseDto(
            response.Id.Value,
            response.SessionId.Value,
            response.ActionId.Value,
            response.OccurrenceId?.Value,
            response.ResponderPersonId,
            response.SelectedOption,
            response.TextResponse,
            response.ModerationStatus,
            response.Upvotes,
            response.SubmittedAtUtc
        );
    }

    private static InteractivePollOptionTallyDto[] ComputePollTallies(
        InteractiveAction action,
        IReadOnlyList<InteractiveResponse> responses
    )
    {
        if (action.ActionType != InteractiveActionType.MultipleChoicePoll)
        {
            return [];
        }

        var totalVotes = responses.Count(static r => !string.IsNullOrWhiteSpace(r.SelectedOption));
        var tallies = new InteractivePollOptionTallyDto[action.Options.Count];
        for (var i = 0; i < action.Options.Count; i++)
        {
            var option = action.Options[i];
            var count = responses.Count(r =>
                string.Equals(r.SelectedOption, option, StringComparison.OrdinalIgnoreCase)
            );
            var percent =
                totalVotes == 0
                    ? 0m
                    : decimal.Round(count * 100m / totalVotes, 2, MidpointRounding.AwayFromZero);
            tallies[i] = new InteractivePollOptionTallyDto(option, count, percent);
        }

        return tallies;
    }
}
