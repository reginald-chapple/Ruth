using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class GetGroupRequirementsQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupRequirementsQuery, IReadOnlyList<GroupRequirementResponse>>
{
    public async Task<Result<IReadOnlyList<GroupRequirementResponse>>> HandleAsync(
        GetGroupRequirementsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var reqQuery = dbContext.GroupRequirements.AsNoTracking().AsQueryable();
        if (query.GroupId.HasValue)
        {
            var groupId = GroupId.From(query.GroupId.Value);
            var groupTypeId = await dbContext
                .Groups.AsNoTracking()
                .Where(g => g.Id == groupId)
                .Select(static g => (GroupTypeId?)g.GroupTypeId)
                .FirstOrDefaultAsync(cancellationToken);
            reqQuery = reqQuery.Where(r =>
                r.GroupId == groupId || (groupTypeId != null && r.GroupTypeId == groupTypeId)
            );
        }
        else if (query.GroupTypeId.HasValue)
        {
            var typeId = GroupTypeId.From(query.GroupTypeId.Value);
            reqQuery = reqQuery.Where(r => r.GroupTypeId == typeId);
        }

        var rows = await reqQuery.ToListAsync(cancellationToken);
        var types = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        return Result.Success<IReadOnlyList<GroupRequirementResponse>>([
            .. rows.Where(r => types.ContainsKey(r.GroupRequirementTypeId))
                .Select(r =>
                    AddGroupRequirementCommandHandler.ToResponse(r, types[r.GroupRequirementTypeId])
                ),
        ]);
    }
}
