using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class FlowAttendanceConversionEvaluator(
    CommunicationDbContext dbContext,
    IClock clock
)
{
    public async Task EvaluateAttendanceConversionAsync(
        Guid personId,
        Guid? groupId,
        CancellationToken cancellationToken
    )
    {
        if (personId == Guid.Empty)
        {
            return;
        }

        var activeRecipients = await dbContext
            .CommunicationFlowInstanceRecipients.Where(r =>
                r.PersonId == personId && r.Status == FlowRecipientStatus.Active
            )
            .ToListAsync(cancellationToken);
        if (activeRecipients.Count == 0)
        {
            return;
        }

        var flowIds = activeRecipients.Select(static r => r.FlowId).Distinct().ToArray();
        var flows = await dbContext
            .CommunicationFlows.AsNoTracking()
            .Where(f => flowIds.Contains(f.Id))
            .ToDictionaryAsync(static f => f.Id, cancellationToken);

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var changed = false;

        foreach (var recipient in activeRecipients)
        {
            if (
                flows.TryGetValue(recipient.FlowId, out var flow)
                && QualifiesForAttendanceGoal(flow, recipient, groupId, nowUtc)
            )
            {
                await ApplyRecipientAttendanceConversionAsync(
                    flow,
                    recipient,
                    groupId,
                    nowUtc,
                    cancellationToken
                );
                changed = true;
            }
        }

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ApplyRecipientAttendanceConversionAsync(
        CommunicationFlow flow,
        CommunicationFlowInstanceRecipient recipient,
        Guid? groupId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        recipient.RecordSignalAndEvaluateExit(
            flow.ExitCondition,
            opened: false,
            clicked: false,
            converted: true,
            nowUtc,
            "AttendedSession"
        );

        var alreadyConverted = await dbContext.CommunicationFlowConversions.AnyAsync(
            c => c.FlowInstanceRecipientId == recipient.Id && c.GoalKind == flow.ConversionGoalKind,
            cancellationToken
        );
        if (!alreadyConverted)
        {
            _ = dbContext.CommunicationFlowConversions.Add(
                CommunicationFlowConversion.Create(
                    recipient,
                    flow.ConversionGoalKind,
                    conversionValue: null,
                    referenceKey: groupId?.ToString("D"),
                    notes: "Auto-converted on PersonAttendedIntegrationEvent",
                    occurredAtUtc: nowUtc
                )
            );
        }
    }

    private static bool QualifiesForAttendanceGoal(
        CommunicationFlow flow,
        CommunicationFlowInstanceRecipient recipient,
        Guid? groupId,
        DateTimeOffset nowUtc
    )
    {
        return nowUtc <= recipient.EnrolledAtUtc.AddDays(flow.ConversionWindowDays)
            && flow.ConversionGoalKind switch
            {
                FlowConversionGoalKind.AttendedSession => true,
                FlowConversionGoalKind.JoinedTargetGroup => groupId.HasValue
                    && flow.ConversionTargetGroupId == groupId.Value,
                FlowConversionGoalKind.None
                or FlowConversionGoalKind.ClickedGoalUrl
                or FlowConversionGoalKind.ManualOrExternalGoal
                or _ => false,
            };
    }
}
