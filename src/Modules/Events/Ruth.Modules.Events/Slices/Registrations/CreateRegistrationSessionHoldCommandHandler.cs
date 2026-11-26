using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class CreateRegistrationSessionHoldCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : ICommandHandler<CreateRegistrationSessionHoldCommand, RegistrationSessionResponse>
{
    public async Task<Result<RegistrationSessionResponse>> HandleAsync(
        CreateRegistrationSessionHoldCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = timeProvider.GetUtcNow();
        var loaded = await LoadOpenInstanceAndTemplateAsync(
            command.InstanceIdOrSlug,
            nowUtc,
            cancellationToken
        );
        if (loaded.IsFailure)
        {
            return Result.Failure<RegistrationSessionResponse>(loaded.Errors);
        }

        var (instance, template) = loaded.Value;
        var (confirmed, held, _, _) = await loader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            excludeSessionToken: null,
            cancellationToken
        );

        var split = RegistrationCapacityAndPricingEngine.AllocateSeatSplit(
            template,
            instance,
            command.RequestedSeatCount,
            confirmed,
            held
        );
        if (split.IsFailure)
        {
            return Result.Failure<RegistrationSessionResponse>(split.Errors);
        }

        var sessionRes = RegistrationSession.CreateHold(
            instance.Id,
            command.RequestedSeatCount,
            split.Value.ConfirmedSeats,
            split.Value.WaitlistSeats,
            template.SeatHoldTtlMinutes,
            nowUtc
        );
        if (sessionRes.IsFailure)
        {
            return Result.Failure<RegistrationSessionResponse>(sessionRes.Errors);
        }

        _ = dbContext.RegistrationSessions.Add(sessionRes.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationMapper.MapSession(sessionRes.Value);
    }

    private async Task<
        Result<(RegistrationInstance Instance, RegistrationTemplate Template)>
    > LoadOpenInstanceAndTemplateAsync(
        string slugOrId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var instance = await loader.LoadInstanceByIdOrSlugAsync(
            slugOrId,
            asNoTracking: true,
            cancellationToken
        );
        if (instance is null)
        {
            return EventsRegistrationErrors.InstanceNotFound;
        }

        if (!instance.IsOpenAt(nowUtc))
        {
            return EventsRegistrationErrors.InstanceClosed;
        }

        var template = await loader.LoadTemplateAsync(
            instance.TemplateId,
            asNoTracking: true,
            cancellationToken
        );
        return template is null ? EventsRegistrationErrors.TemplateNotFound : (instance, template);
    }
}
