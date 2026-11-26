using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Journeys;

internal sealed class UpdateJourneyProgramCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<UpdateJourneyProgramCommand, JourneyProgramResponse>
{
    public async Task<Result<JourneyProgramResponse>> HandleAsync(
        UpdateJourneyProgramCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = JourneyProgramId.From(command.JourneyProgramId);
        var program = await dbContext
            .JourneyPrograms.Include(static p => p.Statuses)
            .Include(static p => p.MilestoneTypes)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (program is null)
        {
            return EngagementErrors.JourneyProgramNotFound;
        }

        var slug = command.Spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var conflict = await dbContext.JourneyPrograms.AnyAsync(
            p => p.Slug == slug && p.Id != id,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.JourneySlugConflict;
        }

        var res = program.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<JourneyProgramResponse>(res.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return JourneyProjectionMapper.ToResponse(program);
    }
}
