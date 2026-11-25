using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Schedules;

internal sealed class CreateScheduleCategoryExclusionCommandHandler(
    CoreDbContext dbContext,
    IClock clock
) : ICommandHandler<CreateScheduleCategoryExclusionCommand, ScheduleCategoryExclusionResponse>
{
    public async Task<Result<ScheduleCategoryExclusionResponse>> HandleAsync(
        CreateScheduleCategoryExclusionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var categoryId = CategoryId.From(command.CategoryId);
        var categoryExists = await dbContext.Categories.AnyAsync(
            c => c.Id == categoryId,
            cancellationToken
        );
        if (!categoryExists)
        {
            return CoreErrors.CategoryNotFound;
        }

        var created = ScheduleCategoryExclusion.Create(
            categoryId,
            command.Title,
            command.StartDate,
            command.EndDate
        );
        if (created.IsFailure)
        {
            return Result.Failure<ScheduleCategoryExclusionResponse>(created.Errors);
        }

        _ = dbContext.ScheduleCategoryExclusions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await ScheduleDateMaterializer.SyncCategorySchedulesAsync(
            dbContext,
            categoryId,
            clock,
            cancellationToken
        );

        return Result.Success(ScheduleMapper.ToResponse(created.Value));
    }
}
