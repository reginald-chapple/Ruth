using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class ProcessDueCommunicationFlowsCommandHandler(
    CommunicationDbContext dbContext,
    FlowStepSenderCollaborator stepSender,
    ITenantContext tenantContext,
    IClock clock
) : ICommandHandler<ProcessDueCommunicationFlowsCommand, FlowAdvancementBatchResultResponse>
{
    public async Task<Result<FlowAdvancementBatchResultResponse>> HandleAsync(
        ProcessDueCommunicationFlowsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!tenantContext.IsLive)
        {
            return BulkCommunicationErrors.TenantNotLive;
        }

        var evalNow = (command.EvaluationTimeOverrideUtc ?? clock.UtcNow).ToUniversalTime();
        var max = Math.Clamp(command.MaxRecipientsPerRun, 1, 500);

        var dueRecipients = await LoadDueRecipientsAsync(
            command.SpecificFlowId,
            evalNow,
            max,
            cancellationToken
        );
        if (dueRecipients.Count == 0)
        {
            return new FlowAdvancementBatchResultResponse(0, 0, 0, 0, 0);
        }

        var flowsById = await LoadFlowsMapAsync(dueRecipients, cancellationToken);
        var stats = await AdvanceBatchAsync(dueRecipients, flowsById, evalNow, cancellationToken);
        await CompleteFinishedCohortInstancesAsync(
            dueRecipients,
            flowsById,
            evalNow,
            cancellationToken
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return stats;
    }

    private Task<List<CommunicationFlowInstanceRecipient>> LoadDueRecipientsAsync(
        Guid? specificFlowId,
        DateTimeOffset evalNow,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var q = dbContext.CommunicationFlowInstanceRecipients.Where(r =>
            r.Status == FlowRecipientStatus.Active
            && r.NextStepDueAtUtc.HasValue
            && r.NextStepDueAtUtc <= evalNow
        );
        if (specificFlowId.HasValue && specificFlowId.Value != Guid.Empty)
        {
            var fid = CommunicationFlowId.From(specificFlowId.Value);
            q = q.Where(r => r.FlowId == fid);
        }

        return q.OrderBy(static r => r.NextStepDueAtUtc).Take(limit).ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<CommunicationFlowId, CommunicationFlow>> LoadFlowsMapAsync(
        List<CommunicationFlowInstanceRecipient> dueRecipients,
        CancellationToken cancellationToken
    )
    {
        var flowIds = dueRecipients.Select(static r => r.FlowId).Distinct().ToArray();
        var flows = await dbContext
            .CommunicationFlows.AsNoTracking()
            .Where(f => flowIds.Contains(f.Id) && f.IsActive)
            .ToListAsync(cancellationToken);
        return flows.ToDictionary(static f => f.Id);
    }

    private async Task<FlowAdvancementBatchResultResponse> AdvanceBatchAsync(
        List<CommunicationFlowInstanceRecipient> dueRecipients,
        Dictionary<CommunicationFlowId, CommunicationFlow> flowsById,
        DateTimeOffset evalNow,
        CancellationToken cancellationToken
    )
    {
        int evaluated = 0,
            sent = 0,
            skippedSmart = 0,
            exitedUnsub = 0,
            completed = 0;

        foreach (var recipient in dueRecipients)
        {
            if (!flowsById.TryGetValue(recipient.FlowId, out var flow))
            {
                continue;
            }

            evaluated++;
            var (stepSent, stepSkipped, stepUnsub, stepDone) =
                await EvaluateSingleRecipientStepAsync(flow, recipient, evalNow, cancellationToken);
            sent += stepSent;
            skippedSmart += stepSkipped;
            exitedUnsub += stepUnsub;
            completed += stepDone;
        }

        return new FlowAdvancementBatchResultResponse(
            evaluated,
            sent,
            skippedSmart,
            exitedUnsub,
            completed
        );
    }

    private async Task<(
        int Sent,
        int SkippedSmart,
        int ExitedUnsub,
        int Completed
    )> EvaluateSingleRecipientStepAsync(
        CommunicationFlow flow,
        CommunicationFlowInstanceRecipient recipient,
        DateTimeOffset evalNow,
        CancellationToken cancellationToken
    )
    {
        if (recipient.CurrentStepIndex >= flow.Steps.Count)
        {
            _ = recipient.AdvanceToNextStep(flow.Steps, evalNow, stepWasSent: false);
            return (0, 0, 0, 1);
        }

        var step = flow.Steps[recipient.CurrentStepIndex];
        if (await stepSender.IsRecipientSuppressedAsync(flow, step, recipient, cancellationToken))
        {
            recipient.MarkUnsubscribedExit("SuppressedOrUnsubscribed", evalNow);
            return (0, 0, 1, 0);
        }

        if (step.SendOnlyIfPreviousUnopened && recipient.OpenedStepsCount > 0)
        {
            var done = recipient.AdvanceToNextStep(flow.Steps, evalNow, stepWasSent: false);
            return (0, 1, 0, done ? 1 : 0);
        }

        var sendRes = await stepSender.SendStepAsync(flow, step, recipient, cancellationToken);
        if (sendRes.IsFailure)
        {
            recipient.MarkFailed(sendRes.PrimaryError.Description, evalNow);
            return (0, 0, 0, 0);
        }

        var finished = recipient.AdvanceToNextStep(flow.Steps, evalNow, stepWasSent: true);
        return (1, 0, 0, finished ? 1 : 0);
    }

    private async Task CompleteFinishedCohortInstancesAsync(
        List<CommunicationFlowInstanceRecipient> dueRecipients,
        Dictionary<CommunicationFlowId, CommunicationFlow> flowsById,
        DateTimeOffset evalNow,
        CancellationToken cancellationToken
    )
    {
        var instanceIds = dueRecipients.Select(static r => r.FlowInstanceId).Distinct().ToArray();
        var instances = await dbContext
            .CommunicationFlowInstances.Where(i =>
                instanceIds.Contains(i.Id) && i.Status == FlowInstanceStatus.Active
            )
            .ToListAsync(cancellationToken);

        foreach (var inst in instances)
        {
            if (
                flowsById.TryGetValue(inst.FlowId, out var flow)
                && flow.TriggerMode == FlowTriggerMode.RollingEnrollment
            )
            {
                continue;
            }

            var anyStillActive =
                dbContext.CommunicationFlowInstanceRecipients.Local.Any(r =>
                    r.FlowInstanceId == inst.Id && r.Status == FlowRecipientStatus.Active
                )
                || await dbContext.CommunicationFlowInstanceRecipients.AnyAsync(
                    r =>
                        r.FlowInstanceId == inst.Id
                        && r.Status == FlowRecipientStatus.Active
                        && !dueRecipients.Select(static d => d.Id).Contains(r.Id),
                    cancellationToken
                );
            if (!anyStillActive)
            {
                inst.MarkCompleted(evalNow);
            }
        }
    }
}
