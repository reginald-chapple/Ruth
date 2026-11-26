using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Achievements;

internal sealed class RecordAchievementProgressCommandHandler(
    EngagementDbContext dbContext,
    AchievementProgressionCollaborator collaborator
) : ICommandHandler<RecordAchievementProgressCommand, AchievementAttemptResponse>
{
    public async Task<Result<AchievementAttemptResponse>> HandleAsync(
        RecordAchievementProgressCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var type = await dbContext
            .AchievementTypes.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == AchievementTypeId.From(command.AchievementTypeId),
                cancellationToken
            );
        if (type is null)
        {
            return EngagementErrors.AchievementTypeNotFound;
        }

        var res = await collaborator.ApplySingleAsync(
            type,
            command.PersonId,
            command.ObservedCount,
            command.AsOfDate,
            cancellationToken
        );
        if (res.IsFailure)
        {
            return Result.Failure<AchievementAttemptResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AchievementProjectionMapper.ToResponse(res.Value.Attempt, res.Value.Type);
    }
}
