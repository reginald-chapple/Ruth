using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationTemplateByIdQueryHandler(RegistrationInventoryLoader loader)
    : IQueryHandler<GetRegistrationTemplateByIdQuery, RegistrationTemplateResponse>
{
    public async Task<Result<RegistrationTemplateResponse>> HandleAsync(
        GetRegistrationTemplateByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var template = await loader.LoadTemplateAsync(
            RegistrationTemplateId.From(query.TemplateId),
            asNoTracking: true,
            cancellationToken
        );
        return template is null
            ? EventsRegistrationErrors.TemplateNotFound
            : RegistrationMapper.MapTemplate(template);
    }
}
