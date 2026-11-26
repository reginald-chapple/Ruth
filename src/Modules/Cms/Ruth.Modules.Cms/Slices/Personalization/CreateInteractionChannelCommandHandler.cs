using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class CreateInteractionChannelCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateInteractionChannelCommand, InteractionChannelResponse>
{
    public async Task<Result<InteractionChannelResponse>> HandleAsync(
        CreateInteractionChannelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = InteractionChannel.Create(
            command.Name,
            command.Key,
            command.ChannelType,
            command.RetentionDurationDays
        );
        if (res.IsFailure)
        {
            return Result.Failure<InteractionChannelResponse>(res.PrimaryError);
        }

        var exists = await db
            .InteractionChannels.AsNoTracking()
            .AnyAsync(c => c.Key == res.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<InteractionChannelResponse>(
                CmsErrors.Conflict(
                    "InteractionChannel.DuplicateKey",
                    $"InteractionChannel key '{res.Value.Key}' already exists."
                )
            );
        }

        _ = db.InteractionChannels.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new InteractionChannelResponse(
                res.Value.Id.Value,
                res.Value.Name,
                res.Value.Key,
                res.Value.ChannelType,
                res.Value.RetentionDurationDays,
                res.Value.IsActive,
                0
            )
        );
    }
}
