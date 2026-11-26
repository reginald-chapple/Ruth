using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class RecordRegistrationPaymentCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    RegistrationFinanceSettlementService financeSettlement,
    TimeProvider timeProvider
) : ICommandHandler<RecordRegistrationPaymentCommand, RegistrationResponse>
{
    public async Task<Result<RegistrationResponse>> HandleAsync(
        RecordRegistrationPaymentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var regId = RegistrationId.From(command.RegistrationId);
        var registration = await dbContext
            .Registrations.Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .Include(static r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == regId, cancellationToken);
        if (registration is null)
        {
            return EventsRegistrationErrors.RegistrationNotFound;
        }

        var instance = await dbContext
            .RegistrationInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == registration.InstanceId, cancellationToken);
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

        var beforeCount = registration.Payments.Count;
        var charged = await financeSettlement.ChargeAndRecordAsync(
            registration,
            template,
            command.Amount,
            command.OneTimeHostedPaymentToken,
            command.SavedPaymentMethodId,
            command.ExternalTransactionCode,
            command.Note ?? $"Installment payment ({registration.ConfirmationCode})",
            timeProvider.GetUtcNow(),
            cancellationToken
        );
        if (charged.IsFailure)
        {
            return Result.Failure<RegistrationResponse>(charged.Errors);
        }

        foreach (var newPayment in registration.Payments.Skip(beforeCount))
        {
            _ = dbContext.RegistrationPaymentRecords.Add(newPayment);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return RegistrationMapper.MapRegistration(registration);
    }
}
