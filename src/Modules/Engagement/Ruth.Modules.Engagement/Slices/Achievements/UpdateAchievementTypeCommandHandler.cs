using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class UpdateAchievementTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<UpdateAchievementTypeCommand, AchievementTypeResponse>
{
    public async Task<Result<AchievementTypeResponse>> HandleAsync(
        UpdateAchievementTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = AchievementTypeId.From(command.AchievementTypeId);
        var entity = await dbContext.AchievementTypes.FirstOrDefaultAsync(
            a => a.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return EngagementErrors.AchievementTypeNotFound;
        }

        var code = command.Spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var conflict = await dbContext.AchievementTypes.AnyAsync(
            a => a.Code == code && a.Id != id,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.AchievementCodeConflict;
        }

        var res = entity.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<AchievementTypeResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AchievementProjectionMapper.ToResponse(entity);
    }
}
