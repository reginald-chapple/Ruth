using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationByConfirmationOrIdQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetRegistrationByConfirmationOrIdQuery, RegistrationResponse>
{
    public async Task<Result<RegistrationResponse>> HandleAsync(
        GetRegistrationByConfirmationOrIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.ConfirmationCodeOrId))
        {
            return EventsRegistrationErrors.RegistrationNotFound;
        }

        var q = dbContext
            .Registrations.AsNoTracking()
            .Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .Include(static r => r.Payments)
            .AsSplitQuery();

        var trimmed = query.ConfirmationCodeOrId.Trim();
        var upperCode = trimmed.ToUpperInvariant();
        var registration = Guid.TryParse(trimmed, out var guid)
            ? await q.FirstOrDefaultAsync(r => r.Id == RegistrationId.From(guid), cancellationToken)
            : await q.FirstOrDefaultAsync(r => r.ConfirmationCode == upperCode, cancellationToken);

        return registration is null
            ? EventsRegistrationErrors.RegistrationNotFound
            : RegistrationMapper.MapRegistration(registration);
    }
}
