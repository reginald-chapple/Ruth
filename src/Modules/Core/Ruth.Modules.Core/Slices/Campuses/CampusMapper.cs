using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Campuses;

internal static class CampusMapper
{
    public static CampusResponse ToResponse(Campus campus)
    {
        return new CampusResponse(
            campus.Id.Value,
            campus.SystemGuid,
            campus.Name,
            campus.CondensedName,
            campus.Description,
            campus.ShortCode,
            campus.IsActive,
            campus.IsSystem,
            campus.Url,
            campus.PhoneNumber,
            campus.TimeZoneId,
            campus.LocationId?.Value,
            campus.LeaderPersonId,
            campus.CampusStatusValueId,
            campus.CampusTypeValueId,
            campus.TeamGroupId,
            campus.PhotoId,
            campus.OpenedDate,
            campus.ClosedDate,
            campus.TitheMetric,
            campus.AverageWeekendAttendance,
            campus.BeaconId,
            campus.Order,
            [
                .. campus
                    .Schedules.OrderBy(static s => s.Order)
                    .Select(static s => new CampusScheduleDto(
                        s.ScheduleId,
                        s.ScheduleTypeValueId,
                        s.Order
                    )),
            ],
            [
                .. campus.Topics.Select(static t => new CampusTopicDto(
                    t.TopicTypeValueId,
                    t.Email,
                    t.IsPublic
                )),
            ],
            campus.CreatedOnUtc,
            campus.ModifiedOnUtc
        );
    }

    public static CampusInput ToInput(CreateCampusCommand cmd)
    {
        return new CampusInput(
            cmd.Name,
            cmd.Description,
            cmd.ShortCode,
            cmd.IsActive,
            cmd.Url,
            cmd.PhoneNumber,
            cmd.TimeZoneId,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.LeaderPersonId,
            cmd.CampusStatusValueId,
            cmd.CampusTypeValueId,
            cmd.PhotoId,
            cmd.OpenedDate,
            cmd.ClosedDate,
            cmd.TitheMetric,
            cmd.AverageWeekendAttendance,
            cmd.BeaconId,
            cmd.Order,
            cmd.Schedules,
            cmd.Topics
        );
    }

    public static CampusInput ToInput(UpdateCampusCommand cmd)
    {
        return new CampusInput(
            cmd.Name,
            cmd.Description,
            cmd.ShortCode,
            cmd.IsActive,
            cmd.Url,
            cmd.PhoneNumber,
            cmd.TimeZoneId,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.LeaderPersonId,
            cmd.CampusStatusValueId,
            cmd.CampusTypeValueId,
            cmd.PhotoId,
            cmd.OpenedDate,
            cmd.ClosedDate,
            cmd.TitheMetric,
            cmd.AverageWeekendAttendance,
            cmd.BeaconId,
            cmd.Order,
            cmd.Schedules,
            cmd.Topics
        );
    }
}
