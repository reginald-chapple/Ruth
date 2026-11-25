using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain.Configurations;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal static class CheckInConfigurationMapper
{
    public static CheckInConfigurationSummaryResponse ToSummary(CheckInConfiguration entity)
    {
        return new CheckInConfigurationSummaryResponse(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.ProfileKind,
            entity.BranchId,
            entity.SearchMode,
            entity.IsActive,
            entity.Areas.Count,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static CheckInConfigurationDetailResponse ToDetail(CheckInConfiguration entity)
    {
        var areas = entity
            .Areas.OrderBy(static a => a.Order)
            .ThenBy(static a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapArea)
            .ToList();

        return new CheckInConfigurationDetailResponse(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.ProfileKind,
            entity.BranchId,
            entity.SearchMode,
            entity.CheckInStartOffsetMinutes,
            entity.CheckInEndOffsetMinutes,
            entity.SecurityCodeAlphaLength,
            entity.SecurityCodeNumericLength,
            entity.PreventDuplicateCheckIn,
            entity.AllowCheckout,
            entity.AutoSelectDaysBack,
            entity.IsActive,
            areas,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    private static CheckInAreaResponse MapArea(CheckInArea area)
    {
        var rooms = area
            .Rooms.OrderBy(static r => r.PriorityOrder)
            .ThenBy(static r => r.LocationName, StringComparer.OrdinalIgnoreCase)
            .Select(static r => new CheckInRoomMatrixCellResponse(
                r.Id.Value,
                r.LocationId,
                r.LocationName,
                r.ScheduleIds,
                r.SoftCapacity,
                r.FirmCapacity,
                r.PriorityOrder,
                r.IsOpenForCheckIn,
                r.IsOverflowRoom
            ))
            .ToList();

        return new CheckInAreaResponse(
            area.Id.Value,
            area.Name,
            area.Description,
            area.LinkedGroupId,
            area.BalancingStrategy,
            area.Order,
            area.IsActive,
            area.PrintBadge,
            area.IssueSecurityPickupCode,
            area.Eligibility.ToDto(),
            rooms
        );
    }
}
