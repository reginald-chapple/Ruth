using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class SkipTouchpointCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<SkipTouchpointCommand, TouchpointResponse>
{
    public async Task<Result<TouchpointResponse>> HandleAsync(
        SkipTouchpointCommand command,
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

        var contact = await dbContext.ConstituentContacts.FirstOrDefaultAsync(
            c => c.Id == ConstituentContactId.From(touchpoint.ConstituentContactId),
            cancellationToken
        );
        if (contact is null)
        {
            return EngagementErrors.ConstituentContactNotFound;
        }

        var res = touchpoint.Skip(command.Reason);
        if (res.IsFailure)
        {
            return Result.Failure<TouchpointResponse>(res.Errors);
        }

        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        contact.AdvanceSkippedCycle(
            touchpoint.ScheduledDate > today ? touchpoint.ScheduledDate : today,
            today
        );

        if (contact.IsActive && contact.NextScheduledTouchpointDate.HasValue)
        {
            var next = Touchpoint.Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                contact.PreferredChannel,
                contact.NextScheduledTouchpointDate.Value,
                $"Scheduled {contact.PortfolioRole} outreach"
            );
            if (next.IsSuccess)
            {
                _ = dbContext.Touchpoints.Add(next.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [touchpoint.ConstituentPersonId],
            cancellationToken
        );
        return TouchpointProjectionMapper.ToResponse(touchpoint, contact.PortfolioRole, people);
    }
}
