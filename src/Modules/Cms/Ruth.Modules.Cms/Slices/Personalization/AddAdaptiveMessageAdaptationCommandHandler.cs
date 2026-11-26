using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class AddAdaptiveMessageAdaptationCommandHandler(CmsDbContext db)
    : ICommandHandler<AddAdaptiveMessageAdaptationCommand, AdaptiveMessageResponse>
{
    public async Task<Result<AdaptiveMessageResponse>> HandleAsync(
        AddAdaptiveMessageAdaptationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = AdaptiveMessageId.From(command.AdaptiveMessageId);
        var message = await db
            .AdaptiveMessages.Include(static m => m.Adaptations)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return Result.Failure<AdaptiveMessageResponse>(
                CmsErrors.NotFound("AdaptiveMessage", command.AdaptiveMessageId.ToString("D"))
            );
        }

        var res = message.AddAdaptation(
            command.Name,
            command.Headline,
            command.BodyMarkup,
            command.Order,
            command.SegmentKeys,
            command.StartDateTimeUtc,
            command.ExpireDateTimeUtc,
            command.ViewSaturationCount,
            command.ViewSaturationInDays,
            command.CallToActionText,
            command.CallToActionUrl,
            command.ImageUrl
        );
        if (res.IsFailure)
        {
            return Result.Failure<AdaptiveMessageResponse>(res.PrimaryError);
        }

        _ = db.AdaptiveMessageAdaptations.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(message));
    }
}
