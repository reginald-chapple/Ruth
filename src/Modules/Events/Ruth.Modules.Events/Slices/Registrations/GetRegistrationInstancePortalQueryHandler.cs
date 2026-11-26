using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationInstancePortalQueryHandler(
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : IQueryHandler<GetRegistrationInstancePortalQuery, PublicRegistrationWizardInitResponse>
{
    public async Task<Result<PublicRegistrationWizardInitResponse>> HandleAsync(
        GetRegistrationInstancePortalQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var instance = await loader.LoadInstanceByIdOrSlugAsync(
            query.InstanceIdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        if (instance is null)
        {
            return EventsRegistrationErrors.InstanceNotFound;
        }

        var template = await loader.LoadTemplateAsync(
            instance.TemplateId,
            asNoTracking: true,
            cancellationToken
        );
        if (template is null)
        {
            return EventsRegistrationErrors.TemplateNotFound;
        }

        var nowUtc = timeProvider.GetUtcNow();
        var (confirmed, held, waitlisted, _) = await loader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            excludeSessionToken: null,
            cancellationToken
        );

        return new PublicRegistrationWizardInitResponse(
            RegistrationMapper.MapInstance(instance, template, confirmed, held, waitlisted, nowUtc),
            RegistrationMapper.MapTemplate(template)
        );
    }
}
