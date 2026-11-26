using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class CreateJourneyProgramCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateJourneyProgramCommand, JourneyProgramResponse>
{
    public async Task<Result<JourneyProgramResponse>> HandleAsync(
        CreateJourneyProgramCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = JourneyProgram.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<JourneyProgramResponse>(created.Errors);
        }

        var conflict = await dbContext.JourneyPrograms.AnyAsync(
            p => p.Slug == created.Value.Slug,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.JourneySlugConflict;
        }

        _ = dbContext.JourneyPrograms.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return JourneyProjectionMapper.ToResponse(created.Value);
    }
}
