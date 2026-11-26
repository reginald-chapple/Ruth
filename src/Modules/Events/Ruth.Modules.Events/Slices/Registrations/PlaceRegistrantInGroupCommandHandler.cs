using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class PlaceRegistrantInGroupCommandHandler(
    EventsDbContext dbContext,
    RegistrationGroupPlacementCollaborator placementCollaborator
) : ICommandHandler<PlaceRegistrantInGroupCommand, RegistrationRegistrantResponse>
{
    public async Task<Result<RegistrationRegistrantResponse>> HandleAsync(
        PlaceRegistrantInGroupCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var regId = RegistrationId.From(command.RegistrationId);
        var registration = await dbContext
            .Registrations.Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .FirstOrDefaultAsync(r => r.Id == regId, cancellationToken);
        if (registration is null)
        {
            return EventsRegistrationErrors.RegistrationNotFound;
        }

        var targetRegistrantId = RegistrationRegistrantId.From(command.RegistrantId);
        var registrant = registration.Registrants.FirstOrDefault(r => r.Id == targetRegistrantId);
        if (registrant is null)
        {
            return EventsRegistrationErrors.RegistrantNotFound;
        }

        var placed = await placementCollaborator.PlaceSingleRegistrantAsync(
            registrant,
            command.TargetGroupId,
            command.TargetGroupRoleId,
            registration.ConfirmationCode,
            cancellationToken
        );
        if (placed.IsFailure)
        {
            return Result.Failure<RegistrationRegistrantResponse>(placed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationMapper.MapRegistrant(registrant);
    }
}
