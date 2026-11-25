using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Domain;

namespace Ruth.Modules.Core.Slices.Branches;

internal static class BranchMapper
{
    public static BranchResponse ToResponse(Branch branch)
    {
        return new BranchResponse(
            branch.Id.Value,
            branch.SystemGuid,
            branch.Name,
            branch.CondensedName,
            branch.Description,
            branch.ShortCode,
            branch.IsActive,
            branch.IsSystem,
            branch.Url,
            branch.PhoneNumber,
            branch.TimeZoneId,
            branch.LocationId?.Value,
            branch.LeaderPersonId,
            branch.BranchStatusValueId,
            branch.BranchTypeValueId,
            branch.TeamGroupId,
            branch.PhotoId,
            branch.OpenedDate,
            branch.ClosedDate,
            branch.BeaconId,
            branch.Order,
            [
                .. branch
                    .Schedules.OrderBy(static s => s.Order)
                    .Select(static s => new BranchScheduleDto(
                        s.ScheduleId,
                        s.ScheduleTypeValueId,
                        s.Order
                    )),
            ],
            [
                .. branch.Topics.Select(static t => new BranchTopicDto(
                    t.TopicTypeValueId,
                    t.Email,
                    t.IsPublic
                )),
            ],
            branch.CreatedOnUtc,
            branch.ModifiedOnUtc
        );
    }

    public static BranchInput ToInput(CreateBranchCommand cmd)
    {
        return new BranchInput(
            cmd.Name,
            cmd.Description,
            cmd.ShortCode,
            cmd.IsActive,
            cmd.Url,
            cmd.PhoneNumber,
            cmd.TimeZoneId,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.LeaderPersonId,
            cmd.BranchStatusValueId,
            cmd.BranchTypeValueId,
            cmd.PhotoId,
            cmd.OpenedDate,
            cmd.ClosedDate,
            cmd.BeaconId,
            cmd.Order,
            cmd.Schedules,
            cmd.Topics
        );
    }

    public static BranchInput ToInput(UpdateBranchCommand cmd)
    {
        return new BranchInput(
            cmd.Name,
            cmd.Description,
            cmd.ShortCode,
            cmd.IsActive,
            cmd.Url,
            cmd.PhoneNumber,
            cmd.TimeZoneId,
            cmd.LocationId.HasValue ? LocationId.From(cmd.LocationId.Value) : null,
            cmd.LeaderPersonId,
            cmd.BranchStatusValueId,
            cmd.BranchTypeValueId,
            cmd.PhotoId,
            cmd.OpenedDate,
            cmd.ClosedDate,
            cmd.BeaconId,
            cmd.Order,
            cmd.Schedules,
            cmd.Topics
        );
    }
}
