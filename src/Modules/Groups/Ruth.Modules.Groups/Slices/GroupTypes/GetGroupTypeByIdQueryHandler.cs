using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.GroupTypes;

internal sealed class GetGroupTypeByIdQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupTypeByIdQuery, GroupTypeDetailResponse>
{
    public async Task<Result<GroupTypeDetailResponse>> HandleAsync(
        GetGroupTypeByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = GroupTypeId.From(query.GroupTypeId);
        var groupType = await dbContext
            .GroupTypes.AsNoTracking()
            .Include(static gt => gt.Roles)
            .Include(static gt => gt.AllowedChildren)
            .FirstOrDefaultAsync(gt => gt.Id == id, cancellationToken);

        return groupType is null
            ? (Result<GroupTypeDetailResponse>)GroupsErrors.GroupTypeNotFound
            : Result.Success(GroupTypeMapper.ToDetail(groupType));
    }
}
