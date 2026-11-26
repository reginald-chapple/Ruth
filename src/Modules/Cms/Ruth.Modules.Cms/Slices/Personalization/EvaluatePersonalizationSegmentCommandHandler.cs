using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class EvaluatePersonalizationSegmentCommandHandler(
    CmsDbContext db,
    PersonalizationSegmentEvaluator evaluator
) : ICommandHandler<EvaluatePersonalizationSegmentCommand, PersonalizationSegmentResponse>
{
    public async Task<Result<PersonalizationSegmentResponse>> HandleAsync(
        EvaluatePersonalizationSegmentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        PersonalizationSegment? segment;
        if (Guid.TryParse(command.SegmentIdOrKey, out var parsedGuid))
        {
            var id = PersonalizationSegmentId.From(parsedGuid);
            segment = await db.PersonalizationSegments.FirstOrDefaultAsync(
                s => s.Id == id,
                cancellationToken
            );
        }
        else
        {
            var key = CmsTextNormalizer.NormalizeKey(command.SegmentIdOrKey);
            segment = await db.PersonalizationSegments.FirstOrDefaultAsync(
                s => s.SegmentKey == key,
                cancellationToken
            );
        }

        if (segment is null)
        {
            return Result.Failure<PersonalizationSegmentResponse>(
                CmsErrors.NotFound("PersonalizationSegment", command.SegmentIdOrKey)
            );
        }

        _ = await evaluator.EvaluateAndSyncAsync(segment, cancellationToken);
        return Result.Success(CmsChannelProjectionMapper.ToResponse(segment));
    }
}
