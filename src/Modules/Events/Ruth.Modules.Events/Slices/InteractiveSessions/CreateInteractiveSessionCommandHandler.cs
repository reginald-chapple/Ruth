using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.Modules.Events.Domain.InteractiveSessions;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.InteractiveSessions;

internal sealed class CreateInteractiveSessionCommandHandler(EventsDbContext dbContext)
    : ICommandHandler<CreateInteractiveSessionCommand, InteractiveSessionDetailResponse>
{
    public async Task<Result<InteractiveSessionDetailResponse>> HandleAsync(
        CreateInteractiveSessionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = InteractiveSession.Create(
            command.Title,
            command.EventItemOccurrenceId,
            command.ModeratorPersonId,
            command.InitialActions
        );
        if (created.IsFailure)
        {
            return Result.Failure<InteractiveSessionDetailResponse>(created.Errors);
        }

        while (
            await dbContext.InteractiveSessions.AnyAsync(
                s => s.JoinCode == created.Value.JoinCode,
                cancellationToken
            )
        )
        {
            created.Value.RegenerateJoinCode();
        }

        _ = dbContext.InteractiveSessions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return InteractiveSessionMapper.MapDetail(created.Value);
    }
}
