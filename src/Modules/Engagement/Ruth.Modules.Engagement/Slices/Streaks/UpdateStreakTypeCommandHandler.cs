using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Streaks;

internal sealed class UpdateStreakTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<UpdateStreakTypeCommand, StreakTypeResponse>
{
    public async Task<Result<StreakTypeResponse>> HandleAsync(
        UpdateStreakTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = StreakTypeId.From(command.StreakTypeId);
        var entity = await dbContext
            .StreakTypes.Include(static s => s.Exclusions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null)
        {
            return EngagementErrors.StreakTypeNotFound;
        }

        var code = command.Spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var conflict = await dbContext.StreakTypes.AnyAsync(
            s => s.Code == code && s.Id != id,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.StreakCodeConflict;
        }

        var res = entity.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<StreakTypeResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return StreakProjectionMapper.ToResponse(entity);
    }
}
