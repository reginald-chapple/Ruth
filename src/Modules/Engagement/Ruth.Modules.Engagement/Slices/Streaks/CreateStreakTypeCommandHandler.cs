using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class CreateStreakTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateStreakTypeCommand, StreakTypeResponse>
{
    public async Task<Result<StreakTypeResponse>> HandleAsync(
        CreateStreakTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = StreakType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<StreakTypeResponse>(created.Errors);
        }

        var conflict = await dbContext.StreakTypes.AnyAsync(
            s => s.Code == created.Value.Code,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.StreakCodeConflict;
        }

        _ = dbContext.StreakTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StreakProjectionMapper.ToResponse(created.Value);
    }
}
