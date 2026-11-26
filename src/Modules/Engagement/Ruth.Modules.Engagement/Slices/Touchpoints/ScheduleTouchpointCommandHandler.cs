using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class ScheduleTouchpointCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : ICommandHandler<ScheduleTouchpointCommand, TouchpointResponse>
{
    public async Task<Result<TouchpointResponse>> HandleAsync(
        ScheduleTouchpointCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var contact = await dbContext
            .ConstituentContacts.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == ConstituentContactId.From(command.ConstituentContactId),
                cancellationToken
            );
        if (contact is null)
        {
            return EngagementErrors.ConstituentContactNotFound;
        }

        var scheduled = Touchpoint.Schedule(
            contact,
            command.Kind,
            command.Channel,
            command.ScheduledDate,
            command.Summary
        );
        if (scheduled.IsFailure)
        {
            return Result.Failure<TouchpointResponse>(scheduled.Errors);
        }

        _ = dbContext.Touchpoints.Add(scheduled.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [contact.ConstituentPersonId],
            cancellationToken
        );
        return TouchpointProjectionMapper.ToResponse(
            scheduled.Value,
            contact.PortfolioRole,
            people
        );
    }
}
