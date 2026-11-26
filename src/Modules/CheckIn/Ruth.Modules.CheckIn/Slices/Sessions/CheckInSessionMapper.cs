using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain.Sessions;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal static class CheckInSessionMapper
{
    public static CheckInSessionReceiptResponse ToReceipt(AttendanceCheckInSession session)
    {
        return new CheckInSessionReceiptResponse(
            session.Id.Value,
            session.CheckInConfigurationId.Value,
            session.ConfigurationName,
            session.Channel,
            session.ServiceDate,
            session.HouseholdId,
            session.SecurityPickupCode,
            session.MobileExpressToken,
            session.CompletedOnUtc,
            [
                .. session
                    .Entries.OrderBy(static e => e.PersonFullName, StringComparer.OrdinalIgnoreCase)
                    .Select(ToEntryReceipt),
            ]
        );
    }

    public static CheckedInEntryReceiptDto ToEntryReceipt(CheckInAttendanceEntry entry)
    {
        return new CheckedInEntryReceiptDto(
            entry.Id.Value,
            entry.PersonId,
            entry.PersonFullName,
            entry.CheckInAreaId.Value,
            entry.AreaName,
            entry.LinkedGroupId,
            entry.LocationId,
            entry.LocationName,
            entry.ScheduleId,
            entry.Status,
            entry.SecurityPickupCode,
            entry.PrintBadgeRequested,
            entry.IsCapacityOverride,
            entry.IsEligibilityOverride,
            entry.AttendanceNote,
            entry.CheckedInOnUtc,
            entry.CheckedOutOnUtc
        );
    }
}
