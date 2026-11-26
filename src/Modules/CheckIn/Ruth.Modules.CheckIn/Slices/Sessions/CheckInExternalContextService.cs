using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class CheckInExternalContextService(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
)
{
    public async Task<(
        string FullName,
        CheckInParticipantContextDto Context
    )> ResolveParticipantAsync(CheckInSelectionItemDto item, CancellationToken cancellationToken)
    {
        var fallbackName = string.IsNullOrWhiteSpace(item.PersonFullName)
            ? "Participant"
            : item.PersonFullName.Trim();
        if (item.ParticipantContext is not null)
        {
            return (fallbackName, item.ParticipantContext);
        }

        var detailResult = await queryDispatcher.QueryAsync<
            GetPersonByIdQuery,
            PersonDetailResponse
        >(new GetPersonByIdQuery(item.PersonId), cancellationToken);
        if (detailResult.IsFailure)
        {
            return (fallbackName, new CheckInParticipantContextDto(PersonId: item.PersonId));
        }

        var p = detailResult.Value;
        int? numericGrade = p.GradeOffset.HasValue ? 12 - p.GradeOffset.Value : null;
        _ = p.Attributes.TryGetValue("MembershipTier", out var tier);
        _ = p.Attributes.TryGetValue("Skills", out var skillsCsv);
        _ = p.Attributes.TryGetValue("NeedsAccommodations", out var accomRaw);
        var needsAccom = bool.TryParse(accomRaw, out var parsedAccom) && parsedAccom;

        return (
            p.FullName,
            new CheckInParticipantContextDto(
                p.Id,
                p.Age,
                p.BirthDate,
                numericGrade,
                tier,
                string.IsNullOrWhiteSpace(skillsCsv)
                    ? []
                    : skillsCsv.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    ),
                needsAccom
            )
        );
    }

    public async Task SynchronizeLinkedGroupAttendanceAsync(
        AttendanceCheckInSession session,
        Guid? checkedInByPersonId,
        CancellationToken cancellationToken
    )
    {
        var grouped = session
            .Entries.Where(static e => e.LinkedGroupId.HasValue)
            .GroupBy(static e => (GroupId: e.LinkedGroupId!.Value, e.LocationId, e.ScheduleId));

        foreach (var bucket in grouped)
        {
            var entries = bucket
                .Select(static e => new GroupAttendanceEntryInputDto(
                    e.PersonId,
                    DidAttend: true,
                    Note: e.AttendanceNote
                ))
                .ToList();

            _ = await commandDispatcher.SendAsync<
                RecordGroupAttendanceCommand,
                AttendanceOccurrenceDetailResponse
            >(
                new RecordGroupAttendanceCommand(
                    bucket.Key.GroupId,
                    session.ServiceDate,
                    bucket.Key.LocationId,
                    bucket.Key.ScheduleId,
                    CheckedInByPersonId: checkedInByPersonId,
                    Entries: entries
                ),
                cancellationToken
            );
        }
    }
}
