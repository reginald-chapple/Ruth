using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class CreatePersonalizationSegmentCommandHandler(
    CmsDbContext db,
    PersonalizationSegmentEvaluator evaluator
) : ICommandHandler<CreatePersonalizationSegmentCommand, PersonalizationSegmentResponse>
{
    public async Task<Result<PersonalizationSegmentResponse>> HandleAsync(
        CreatePersonalizationSegmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = PersonalizationSegment.Create(
            command.Name,
            command.SegmentKey,
            command.Description,
            command.TimeToUpdateIntervalMinutes,
            command.FilterSpec
        );
        if (res.IsFailure)
        {
            return Result.Failure<PersonalizationSegmentResponse>(res.PrimaryError);
        }

        var exists = await db
            .PersonalizationSegments.AsNoTracking()
            .AnyAsync(s => s.SegmentKey == res.Value.SegmentKey, cancellationToken);
        if (exists)
        {
            return Result.Failure<PersonalizationSegmentResponse>(
                CmsErrors.Conflict(
                    "PersonalizationSegment.DuplicateKey",
                    $"Segment key '{res.Value.SegmentKey}' already exists."
                )
            );
        }

        _ = db.PersonalizationSegments.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);

        if (command.EvaluateImmediately)
        {
            _ = await evaluator.EvaluateAndSyncAsync(res.Value, cancellationToken);
        }

        return Result.Success(CmsChannelProjectionMapper.ToResponse(res.Value));
    }
}
