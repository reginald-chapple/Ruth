using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class GetBranchByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetBranchByIdQuery, BranchResponse>
{
    public async Task<Result<BranchResponse>> HandleAsync(
        GetBranchByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = BranchId.From(query.BranchId);
        var branch = await dbContext
            .Branches.AsNoTracking()
            .Include(static b => b.Schedules)
            .Include(static b => b.Topics)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return branch is null
            ? CoreLocationErrors.BranchNotFound
            : Result.Success(BranchMapper.ToResponse(branch));
    }
}
