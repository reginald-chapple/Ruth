using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Domain.Following;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Following;

internal sealed class ToggleFollowingSubscriptionCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<ToggleFollowingSubscriptionCommand, FollowingSubscriptionResponse>
{
    public async Task<Result<FollowingSubscriptionResponse>> HandleAsync(
        ToggleFollowingSubscriptionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var key = command.EntityTypeKey?.Trim() ?? string.Empty;
        var purpose = string.IsNullOrWhiteSpace(command.PurposeKey)
            ? "default"
            : command.PurposeKey.Trim().ToLowerInvariant();

        var existing = await dbContext.FollowingSubscriptions.FirstOrDefaultAsync(
            f =>
                f.FollowerPersonId == command.FollowerPersonId
                && f.EntityTypeKey == key
                && f.EntityId == command.EntityId
                && f.PurposeKey == purpose,
            cancellationToken
        );

        if (existing is not null)
        {
            existing.SetSubscriptionState(command.Subscribe, command.EntityDisplayName);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return ToResponse(existing);
        }

        var created = FollowingSubscription.Create(
            command.FollowerPersonId,
            key,
            command.EntityId,
            command.EntityDisplayName,
            purpose,
            command.Subscribe
        );
        if (created.IsFailure)
        {
            return Result.Failure<FollowingSubscriptionResponse>(created.Errors);
        }

        _ = dbContext.FollowingSubscriptions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(created.Value);
    }

    internal static FollowingSubscriptionResponse ToResponse(FollowingSubscription entity)
    {
        return new(
            entity.Id.Value,
            entity.FollowerPersonId,
            entity.EntityTypeKey,
            entity.EntityId,
            entity.EntityDisplayName,
            entity.PurposeKey,
            entity.IsSubscribed,
            entity.LastNotifiedAtUtc
        );
    }
}
