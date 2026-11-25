using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetScheduleTemplatesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetScheduleTemplatesQuery, IReadOnlyList<GroupMemberScheduleTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<GroupMemberScheduleTemplateResponse>>> HandleAsync(
        GetScheduleTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.ScheduleTemplates.AsNoTracking().AsQueryable();
        if (query.GroupTypeId.HasValue)
        {
            var typeId = GroupTypeId.From(query.GroupTypeId.Value);
            q = q.Where(t => t.GroupTypeId == null || t.GroupTypeId == typeId);
        }

        var rows = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<GroupMemberScheduleTemplateResponse>>([
            .. rows.Select(CreateScheduleTemplateCommandHandler.ToResponse),
        ]);
    }
}
