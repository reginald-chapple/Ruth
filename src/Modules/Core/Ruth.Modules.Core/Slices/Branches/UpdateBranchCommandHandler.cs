using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class UpdateBranchCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UpdateBranchCommand, BranchResponse>
{
    public async Task<Result<BranchResponse>> HandleAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = BranchId.From(command.BranchId);
        var branch = await dbContext
            .Branches.Include(static b => b.Schedules)
            .Include(static b => b.Topics)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (branch is null)
        {
            return CoreLocationErrors.BranchNotFound;
        }

        var updated = branch.Update(BranchMapper.ToInput(command));
        if (updated.IsFailure)
        {
            return Result.Failure<BranchResponse>(updated.Errors);
        }

        var duplicate = await dbContext.Branches.AnyAsync(
            b => b.Id != id && b.NormalizedName == branch.NormalizedName,
            cancellationToken
        );
        if (duplicate)
        {
            return CoreLocationErrors.BranchDuplicateName;
        }

        if (branch.LocationId.HasValue)
        {
            var locationId = branch.LocationId.Value;
            var locationExists = await dbContext.Locations.AnyAsync(
                l => l.Id == locationId,
                cancellationToken
            );
            if (!locationExists)
            {
                return CoreLocationErrors.LocationNotFound;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(BranchMapper.ToResponse(branch));
    }
}
