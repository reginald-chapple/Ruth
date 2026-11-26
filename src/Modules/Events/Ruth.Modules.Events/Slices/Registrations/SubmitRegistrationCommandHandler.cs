using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Events;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class SubmitRegistrationCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    RegistrationFinanceSettlementService financeSettlement,
    RegistrationGroupPlacementCollaborator groupPlacement,
    TimeProvider timeProvider
) : ICommandHandler<SubmitRegistrationCommand, RegistrationResponse>
{
    public async Task<Result<RegistrationResponse>> HandleAsync(
        SubmitRegistrationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = timeProvider.GetUtcNow();
        var ctxRes = await LoadSubmissionContextAsync(command, nowUtc, cancellationToken);
        if (ctxRes.IsFailure)
        {
            return Result.Failure<RegistrationResponse>(ctxRes.Errors);
        }

        var (instance, template, sessionHold, plan) = ctxRes.Value;
        var regRes = Registration.Create(
            instance.Id,
            command.RegistrarPersonId,
            command.RegistrarFirstName,
            command.RegistrarLastName,
            command.RegistrarEmail,
            template.CurrencyCode,
            plan,
            nowUtc
        );
        if (regRes.IsFailure)
        {
            return Result.Failure<RegistrationResponse>(regRes.Errors);
        }

        var registration = regRes.Value;
        var settled = await financeSettlement.SettleInitialSubmissionAsync(
            registration,
            instance,
            template,
            command,
            plan.MinimumInitialAmountDue,
            nowUtc,
            cancellationToken
        );
        if (settled.IsFailure)
        {
            return Result.Failure<RegistrationResponse>(settled.Errors);
        }

        await groupPlacement.AutoPlaceConfirmedRegistrantsAsync(
            registration,
            instance,
            template,
            cancellationToken
        );

        await PersistRegistrationAsync(
            registration,
            instance,
            sessionHold,
            plan,
            nowUtc,
            cancellationToken
        );
        return RegistrationMapper.MapRegistration(registration);
    }

    private async Task<
        Result<(
            RegistrationInstance Instance,
            RegistrationTemplate Template,
            RegistrationSession? Hold,
            EvaluatedRegistrationPlan Plan
        )>
    > LoadSubmissionContextAsync(
        SubmitRegistrationCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var instance = await loader.LoadInstanceByIdOrSlugAsync(
            command.InstanceIdOrSlug,
            asNoTracking: false,
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

        var sessionRes = await ResolveOptionalSessionHoldAsync(
            instance.Id,
            command.SessionToken,
            nowUtc,
            cancellationToken
        );
        if (sessionRes.IsFailure)
        {
            return Result.Failure<(
                RegistrationInstance,
                RegistrationTemplate,
                RegistrationSession?,
                EvaluatedRegistrationPlan
            )>(sessionRes.Errors);
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

        var planRes = await EvaluatePlanAsync(
            template,
            instance,
            command,
            nowUtc,
            cancellationToken
        );
        return planRes.IsFailure
            ? Result.Failure<(
                RegistrationInstance,
                RegistrationTemplate,
                RegistrationSession?,
                EvaluatedRegistrationPlan
            )>(planRes.Errors)
            : (instance, template, sessionRes.Value, planRes.Value);
    }

    private async Task PersistRegistrationAsync(
        Registration registration,
        RegistrationInstance instance,
        RegistrationSession? sessionHold,
        EvaluatedRegistrationPlan plan,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        sessionHold?.MarkCompleted(registration.Id);
        _ = dbContext.Registrations.Add(registration);
        dbContext.EnqueueIntegrationEvent(
            new EventRegistrationCompletedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                registration.Id.Value,
                instance.Id.Value,
                registration.ConfirmationCode,
                registration.RegistrarPersonId,
                registration.RegistrarEmail,
                plan.ConfirmedCount,
                plan.WaitlistedCount,
                registration.TotalAmount,
                registration.PaidAmount,
                registration.LinkedInvoiceId
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Result<RegistrationSession?>> ResolveOptionalSessionHoldAsync(
        RegistrationInstanceId instanceId,
        string? sessionToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            return Result.Success<RegistrationSession?>(value: null);
        }

        var trimmed = sessionToken.Trim();
        var session = await dbContext.RegistrationSessions.FirstOrDefaultAsync(
            s => s.InstanceId == instanceId && s.SessionToken == trimmed,
            cancellationToken
        );
        return session is null || !session.IsActiveAt(nowUtc)
            ? EventsRegistrationErrors.SessionHoldExpired
            : Result.Success<RegistrationSession?>(session);
    }

    private async Task<Result<EvaluatedRegistrationPlan>> EvaluatePlanAsync(
        RegistrationTemplate template,
        RegistrationInstance instance,
        SubmitRegistrationCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var (confirmed, held, _, maxWait) = await loader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            command.SessionToken,
            cancellationToken
        );
        var feeQuantities = await loader.LoadConsumedFeeItemQuantitiesAsync(
            instance.Id,
            cancellationToken
        );
        var discountUsed = await loader.CountDiscountedRegistrantsAsync(
            instance.Id,
            command.DiscountCode,
            cancellationToken
        );

        return RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            template,
            instance,
            command.Registrants,
            confirmed,
            held,
            maxWait,
            feeQuantities,
            discountUsed,
            command.DiscountCode,
            DateOnly.FromDateTime(nowUtc.UtcDateTime)
        );
    }
}
