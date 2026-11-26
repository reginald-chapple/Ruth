using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain.InteractiveSessions;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal static class InteractiveSessionMapper
{
    public static InteractiveSessionDetailResponse MapDetail(InteractiveSession session)
    {
        var actions = session
            .Actions.OrderBy(static a => a.DisplayOrder)
            .Select(InteractiveStageProjectionCalculator.MapAction)
            .ToArray();

        var occurrences = session
            .Occurrences.OrderByDescending(static o => o.StartedAtUtc)
            .Select(o => new InteractiveSessionOccurrenceResponse(
                o.Id.Value,
                o.SessionId.Value,
                o.EventItemOccurrenceId,
                o.BranchId,
                o.StartedAtUtc,
                o.EndedAtUtc,
                session.Responses.Count(r => r.OccurrenceId == o.Id)
            ))
            .ToArray();

        var responses = session
            .Responses.OrderByDescending(static r => r.SubmittedAtUtc)
            .Select(InteractiveStageProjectionCalculator.MapResponse)
            .ToArray();

        return new InteractiveSessionDetailResponse(
            session.Id.Value,
            session.Title,
            session.JoinCode,
            session.EventItemOccurrenceId,
            session.ModeratorPersonId,
            session.Status,
            session.ActiveActionId?.Value,
            session.ActiveOccurrenceId?.Value,
            session.StartedAtUtc,
            session.EndedAtUtc,
            actions,
            occurrences,
            responses
        );
    }
}
