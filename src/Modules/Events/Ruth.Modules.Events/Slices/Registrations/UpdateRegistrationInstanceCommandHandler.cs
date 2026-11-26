using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class UpdateRegistrationInstanceCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : ICommandHandler<UpdateRegistrationInstanceCommand, RegistrationInstanceResponse>
{
    public async Task<Result<RegistrationInstanceResponse>> HandleAsync(
        UpdateRegistrationInstanceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = RegistrationInstanceId.From(command.InstanceId);
        var instance = await dbContext.RegistrationInstances.FirstOrDefaultAsync(
            i => i.Id == targetId,
            cancellationToken
        );
        if (instance is null)
        {
            return EventsRegistrationErrors.InstanceNotFound;
        }

        var template = await loader.LoadTemplateAsync(
            RegistrationTemplateId.From(command.Spec.TemplateId),
            asNoTracking: true,
            cancellationToken
        );
        if (template is null)
        {
            return EventsRegistrationErrors.TemplateNotFound;
        }

        var updated = instance.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<RegistrationInstanceResponse>(updated.Errors);
        }

        var slugConflict = await dbContext.RegistrationInstances.AnyAsync(
            i => i.Id != targetId && i.Slug == instance.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return EventsRegistrationErrors.InstanceSlugConflict;
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var nowUtc = timeProvider.GetUtcNow();
        var (confirmed, held, waitlisted, _) = await loader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            excludeSessionToken: null,
            cancellationToken
        );
        return RegistrationMapper.MapInstance(
            instance,
            template,
            confirmed,
            held,
            waitlisted,
            nowUtc
        );
    }
}
