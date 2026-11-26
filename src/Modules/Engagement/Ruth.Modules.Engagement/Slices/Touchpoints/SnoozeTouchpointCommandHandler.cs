using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class SnoozeTouchpointCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<SnoozeTouchpointCommand, TouchpointResponse>
{
    public async Task<Result<TouchpointResponse>> HandleAsync(
        SnoozeTouchpointCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var touchpoint = await dbContext.Touchpoints.FirstOrDefaultAsync(
            t => t.Id == TouchpointId.From(command.TouchpointId),
            cancellationToken
        );
        if (touchpoint is null)
        {
            return EngagementErrors.TouchpointNotFound;
        }

        var res = touchpoint.Snooze(command.SnoozeUntilDate, command.Note);
        if (res.IsFailure)
        {
            return Result.Failure<TouchpointResponse>(res.Errors);
        }

        var role = await dbContext
            .ConstituentContacts.AsNoTracking()
            .Where(c => c.Id == ConstituentContactId.From(touchpoint.ConstituentContactId))
            .Select(static c => c.PortfolioRole)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Steward";

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [touchpoint.ConstituentPersonId],
            cancellationToken
        );
        return TouchpointProjectionMapper.ToResponse(touchpoint, role, people);
    }
}
