using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Requirements;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Requirements;

internal sealed class GetGroupRequirementTypesQueryHandler(GroupsDbContext dbContext)
    : IQueryHandler<GetGroupRequirementTypesQuery, IReadOnlyList<GroupRequirementTypeResponse>>
{
    public async Task<Result<IReadOnlyList<GroupRequirementTypeResponse>>> HandleAsync(
        GetGroupRequirementTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var items = await dbContext
            .GroupRequirementTypes.AsNoTracking()
            .OrderBy(static t => t.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GroupRequirementTypeResponse>>([
            .. items.Select(CreateGroupRequirementTypeCommandHandler.ToResponse),
        ]);
    }
}
