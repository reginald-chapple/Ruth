using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Achievements;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class CreateAchievementTypeCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateAchievementTypeCommand, AchievementTypeResponse>
{
    public async Task<Result<AchievementTypeResponse>> HandleAsync(
        CreateAchievementTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = AchievementType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<AchievementTypeResponse>(created.Errors);
        }

        var conflict = await dbContext.AchievementTypes.AnyAsync(
            a => a.Code == created.Value.Code,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.AchievementCodeConflict;
        }

        _ = dbContext.AchievementTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AchievementProjectionMapper.ToResponse(created.Value);
    }
}
