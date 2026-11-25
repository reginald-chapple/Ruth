using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class DeleteBranchCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteBranchCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteBranchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = BranchId.From(command.BranchId);
        var branch = await dbContext.Branches.FirstOrDefaultAsync(
            b => b.Id == id,
            cancellationToken
        );
        if (branch is null)
        {
            return CoreLocationErrors.BranchNotFound;
        }

        if (branch.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var totalBranches = await dbContext.Branches.CountAsync(cancellationToken);
        if (totalBranches <= 1)
        {
            return CoreLocationErrors.BranchLastCannotDelete;
        }

        _ = dbContext.Branches.Remove(branch);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
