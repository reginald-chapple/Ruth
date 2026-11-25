using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetGroupScheduleExclusionsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupScheduleExclusionsQuery, IReadOnlyList<GroupScheduleExclusionResponse>>
{
    public async Task<Result<IReadOnlyList<GroupScheduleExclusionResponse>>> HandleAsync(
        GetGroupScheduleExclusionsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var typeId = GroupTypeId.From(query.GroupTypeId);
        var rows = await dbContext
            .GroupScheduleExclusions.AsNoTracking()
            .Where(e => e.GroupTypeId == typeId)
            .OrderBy(static e => e.StartDate)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupScheduleExclusionResponse>>([
            .. rows.Select(AddGroupScheduleExclusionCommandHandler.ToResponse),
        ]);
    }
}
