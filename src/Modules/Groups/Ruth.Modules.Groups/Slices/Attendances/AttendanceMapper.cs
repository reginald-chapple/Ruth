using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Domain.Attendances;

namespace Ruth.Modules.Groups.Slices.Attendances;

internal static class AttendanceMapper
{
    public static AttendanceRecordResponse ToRecord(Attendance row)
    {
        return new AttendanceRecordResponse(
            row.Id.Value,
            row.OccurrenceId.Value,
            row.PersonId,
            row.CampusId,
            row.DeviceId,
            row.StartDateTimeUtc,
            row.EndDateTimeUtc,
            row.DidAttend,
            row.IsFirstTime,
            row.Note,
            row.Rsvp,
            row.RequestedToAttend,
            row.ScheduledToAttend
        );
    }

    public static AttendanceOccurrenceSummaryResponse ToSummary(
        AttendanceOccurrence occ,
        string? groupName
    )
    {
        var present = occ.Attendances.Count(static a => a.DidAttend == true);
        var firstTime = occ.Attendances.Count(static a =>
            a.DidAttend == true && a.IsFirstTime == true
        );
        return new AttendanceOccurrenceSummaryResponse(
            occ.Id.Value,
            occ.GroupId?.Value,
            groupName,
            occ.RootGroupTypeId?.Value,
            occ.LocationId,
            occ.ScheduleId,
            occ.OccurrenceDate,
            occ.SundayDate,
            occ.OccurrenceDateKey,
            occ.Name,
            occ.DidNotOccur,
            occ.AnonymousAttendanceCount,
            present,
            occ.Attendances.Count,
            firstTime,
            occ.Notes
        );
    }

    public static AttendanceOccurrenceDetailResponse ToDetail(
        AttendanceOccurrence occ,
        string? groupName
    )
    {
        var present = occ.Attendances.Count(static a => a.DidAttend == true);
        var firstTime = occ.Attendances.Count(static a =>
            a.DidAttend == true && a.IsFirstTime == true
        );
        return new AttendanceOccurrenceDetailResponse(
            occ.Id.Value,
            occ.GroupId?.Value,
            groupName,
            occ.RootGroupTypeId?.Value,
            occ.LocationId,
            occ.ScheduleId,
            occ.OccurrenceDate,
            occ.SundayDate,
            occ.OccurrenceDateKey,
            occ.Name,
            occ.DidNotOccur,
            occ.AnonymousAttendanceCount,
            present,
            firstTime,
            occ.Notes,
            [.. occ.Attendances.OrderBy(static a => a.StartDateTimeUtc).Select(ToRecord)]
        );
    }
}
