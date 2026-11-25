using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class CreateBranchCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateBranchCommand, BranchResponse>
{
    public async Task<Result<BranchResponse>> HandleAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Branch.Create(BranchMapper.ToInput(command));
        if (created.IsFailure)
        {
            return Result.Failure<BranchResponse>(created.Errors);
        }

        var normalizedName = created.Value.NormalizedName;
        var exists = await dbContext.Branches.AnyAsync(
            b => b.NormalizedName == normalizedName,
            cancellationToken
        );
        if (exists)
        {
            return CoreLocationErrors.BranchDuplicateName;
        }

        if (created.Value.LocationId.HasValue)
        {
            var locationId = created.Value.LocationId.Value;
            var locationExists = await dbContext.Locations.AnyAsync(
                l => l.Id == locationId,
                cancellationToken
            );
            if (!locationExists)
            {
                return CoreLocationErrors.LocationNotFound;
            }
        }

        _ = dbContext.Branches.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(BranchMapper.ToResponse(created.Value));
    }
}
