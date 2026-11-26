using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class CreateRegistrationInstanceCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : ICommandHandler<CreateRegistrationInstanceCommand, RegistrationInstanceResponse>
{
    public async Task<Result<RegistrationInstanceResponse>> HandleAsync(
        CreateRegistrationInstanceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = RegistrationInstance.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<RegistrationInstanceResponse>(created.Errors);
        }

        var template = await loader.LoadTemplateAsync(
            created.Value.TemplateId,
            asNoTracking: true,
            cancellationToken
        );
        if (template is null)
        {
            return EventsRegistrationErrors.TemplateNotFound;
        }

        var slugConflict = await dbContext.RegistrationInstances.AnyAsync(
            i => i.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return EventsRegistrationErrors.InstanceSlugConflict;
        }

        _ = dbContext.RegistrationInstances.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return RegistrationMapper.MapInstance(
            created.Value,
            template,
            confirmedCount: 0,
            activeHeldSeats: 0,
            waitlistedCount: 0,
            timeProvider.GetUtcNow()
        );
    }
}
