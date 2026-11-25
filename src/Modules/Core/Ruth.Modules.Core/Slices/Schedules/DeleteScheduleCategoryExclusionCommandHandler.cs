using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class DeleteScheduleCategoryExclusionCommandHandler(
    CoreDbContext dbContext,
    IClock clock
) : ICommandHandler<DeleteScheduleCategoryExclusionCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteScheduleCategoryExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = ScheduleCategoryExclusionId.From(command.ExclusionId);
        var exclusion = await dbContext.ScheduleCategoryExclusions.FirstOrDefaultAsync(
            e => e.Id == id,
            cancellationToken
        );
        if (exclusion is null)
        {
            return CoreScheduleErrors.ExclusionNotFound;
        }

        var categoryId = exclusion.CategoryId;
        _ = dbContext.ScheduleCategoryExclusions.Remove(exclusion);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleDateMaterializer.SyncCategorySchedulesAsync(
            dbContext,
            categoryId,
            clock,
            cancellationToken
        );

        return Result.Success(value: true);
    }
}
