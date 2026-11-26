using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Domain.Invoices;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class RecordAssistanceDisbursementCommandHandler(
    FinanceDbContext dbContext,
    IClock clock
) : ICommandHandler<RecordAssistanceDisbursementCommand, AssistanceCaseResponse>
{
    public async Task<Result<AssistanceCaseResponse>> HandleAsync(
        RecordAssistanceDisbursementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var caseId = AssistanceCaseId.From(command.CaseId);
        var entity = await dbContext
            .AssistanceCases.Include(static c => c.Documents)
            .Include(static c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (entity is null)
        {
            return FinanceErrors.AssistanceCaseNotFound;
        }

        var program = await dbContext.AssistancePrograms.FirstOrDefaultAsync(
            p => p.Id == entity.ProgramId,
            cancellationToken
        );
        if (program is null)
        {
            return FinanceErrors.AssistanceProgramNotFound;
        }

        var invResult = await ApplyOptionalInvoiceWaiverCreditAsync(command, cancellationToken);
        return invResult.IsFailure
            ? Result.Failure<AssistanceCaseResponse>(invResult.Errors)
            : await CommitAndPersistDisbursementAsync(
                entity,
                program,
                command,
                invResult.Value,
                cancellationToken
            );
    }

    private async Task<Result<AssistanceCaseResponse>> CommitAndPersistDisbursementAsync(
        AssistanceCase entity,
        AssistanceProgram program,
        RecordAssistanceDisbursementCommand command,
        InvoiceId? targetInvoiceId,
        CancellationToken cancellationToken
    )
    {
        var budgetCommit = program.CommitDisbursement(command.Amount);
        if (budgetCommit.IsFailure)
        {
            return Result.Failure<AssistanceCaseResponse>(budgetCommit.Errors);
        }

        var nowUtc = clock.UtcNow;
        var disbursed = entity.RecordDisbursement(
            command.DisbursementMethod,
            command.Amount,
            command.PayeeName,
            targetInvoiceId,
            command.ReferenceNumber,
            command.Notes,
            command.ApprovedByPersonId,
            nowUtc
        );
        if (disbursed.IsFailure)
        {
            return Result.Failure<AssistanceCaseResponse>(disbursed.Errors);
        }

        _ = dbContext.AssistanceDisbursements.Add(disbursed.Value);
        dbContext.EnqueueIntegrationEvent(
            new AssistanceDisbursementRecordedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                entity.Id.Value,
                entity.CaseNumber,
                program.Id.Value,
                entity.ApplicantPersonId,
                command.DisbursementMethod,
                command.Amount,
                entity.CurrencyCode,
                targetInvoiceId?.Value
            )
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(entity, program.Name, program.Category);
    }

    private async Task<Result<InvoiceId?>> ApplyOptionalInvoiceWaiverCreditAsync(
        RecordAssistanceDisbursementCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.TargetInvoiceId is null || command.TargetInvoiceId.Value == Guid.Empty)
        {
            return Result.Success<InvoiceId?>(value: null);
        }

        var invId = InvoiceId.From(command.TargetInvoiceId.Value);
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(
            i => i.Id == invId,
            cancellationToken
        );
        if (invoice is null)
        {
            return FinanceErrors.InvoiceNotFound;
        }

        if (command.DisbursementMethod == AssistanceDisbursementMethod.InternalFeeWaiverCredit)
        {
            var paid = invoice.ApplyPayment(command.Amount);
            if (paid.IsFailure)
            {
                return Result.Failure<InvoiceId?>(paid.Errors);
            }

            if (paid.Value)
            {
                dbContext.EnqueueIntegrationEvent(
                    new InvoiceSettledIntegrationEvent(
                        Guid.CreateVersion7(),
                        clock.UtcNow,
                        invoice.Id.Value,
                        invoice.InvoiceNumber,
                        invoice.PersonId,
                        invoice.PaidAmount,
                        invoice.CurrencyCode
                    )
                );
            }
        }

        return invId;
    }
}
