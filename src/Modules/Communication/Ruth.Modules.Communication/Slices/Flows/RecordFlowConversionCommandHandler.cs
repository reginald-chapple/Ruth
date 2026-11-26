using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class RecordFlowConversionCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<RecordFlowConversionCommand, CommunicationFlowConversionResponse>
{
    public async Task<Result<CommunicationFlowConversionResponse>> HandleAsync(
        RecordFlowConversionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var rid = CommunicationFlowInstanceRecipientId.From(command.FlowInstanceRecipientId);
        var recipient = await dbContext.CommunicationFlowInstanceRecipients.FirstOrDefaultAsync(
            r => r.Id == rid,
            cancellationToken
        );
        if (recipient is null)
        {
            return Result.Failure<CommunicationFlowConversionResponse>(
                CommunicationFlowErrors.FlowRecipientNotFound
            );
        }

        var flow = await dbContext
            .CommunicationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == recipient.FlowId, cancellationToken);
        if (flow is null)
        {
            return Result.Failure<CommunicationFlowConversionResponse>(
                CommunicationFlowErrors.FlowNotFound
            );
        }

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var isConverted = command.GoalKind != FlowConversionGoalKind.None;
        recipient.RecordSignalAndEvaluateExit(
            flow.ExitCondition,
            command.RecordStepOpen,
            command.RecordStepClick,
            isConverted,
            nowUtc,
            $"Goal:{command.GoalKind}"
        );

        if (!isConverted)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return new CommunicationFlowConversionResponse(
                Guid.Empty,
                recipient.FlowId.Value,
                recipient.FlowInstanceId.Value,
                recipient.Id.Value,
                recipient.PersonId,
                FlowConversionGoalKind.None,
                Math.Max(0, recipient.CurrentStepIndex - 1),
                ConversionValue: null,
                command.ReferenceKey,
                nowUtc,
                command.Notes
            );
        }

        return await PersistConversionAsync(command, recipient, nowUtc, cancellationToken);
    }

    private async Task<CommunicationFlowConversionResponse> PersistConversionAsync(
        RecordFlowConversionCommand command,
        CommunicationFlowInstanceRecipient recipient,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.CommunicationFlowConversions.FirstOrDefaultAsync(
            c => c.FlowInstanceRecipientId == recipient.Id && c.GoalKind == command.GoalKind,
            cancellationToken
        );
        if (existing is not null)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return CommunicationFlowMapper.ToResponse(existing);
        }

        var conversion = CommunicationFlowConversion.Create(
            recipient,
            command.GoalKind,
            command.ConversionValue,
            command.ReferenceKey,
            command.Notes,
            nowUtc
        );
        _ = dbContext.CommunicationFlowConversions.Add(conversion);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CommunicationFlowMapper.ToResponse(conversion);
    }
}
