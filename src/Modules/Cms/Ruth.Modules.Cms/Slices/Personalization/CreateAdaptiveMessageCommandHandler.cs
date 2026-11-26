using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class CreateAdaptiveMessageCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateAdaptiveMessageCommand, AdaptiveMessageResponse>
{
    public async Task<Result<AdaptiveMessageResponse>> HandleAsync(
        CreateAdaptiveMessageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = AdaptiveMessage.Create(
            command.Name,
            command.Key,
            command.Description,
            command.CategoryId,
            command.StartDateTimeUtc,
            command.ExpireDateTimeUtc,
            command.IsActive
        );
        if (res.IsFailure)
        {
            return Result.Failure<AdaptiveMessageResponse>(res.PrimaryError);
        }

        var exists = await db
            .AdaptiveMessages.AsNoTracking()
            .AnyAsync(m => m.Key == res.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<AdaptiveMessageResponse>(
                CmsErrors.Conflict(
                    "AdaptiveMessage.DuplicateKey",
                    $"AdaptiveMessage key '{res.Value.Key}' already exists."
                )
            );
        }

        _ = db.AdaptiveMessages.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(res.Value));
    }
}
