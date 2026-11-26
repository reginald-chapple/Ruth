using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class GetCommunicationFlowAnalyticsQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationFlowAnalyticsQuery, CommunicationFlowAnalyticsResponse>
{
    public async Task<Result<CommunicationFlowAnalyticsResponse>> HandleAsync(
        GetCommunicationFlowAnalyticsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var flowId = CommunicationFlowId.From(query.FlowId);
        var flow = await dbContext
            .CommunicationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken);
        if (flow is null)
        {
            return Result.Failure<CommunicationFlowAnalyticsResponse>(
                CommunicationFlowErrors.FlowNotFound
            );
        }

        var instanceCount = await dbContext.CommunicationFlowInstances.CountAsync(
            i => i.FlowId == flowId,
            cancellationToken
        );
        var recipients = await dbContext
            .CommunicationFlowInstanceRecipients.AsNoTracking()
            .Where(r => r.FlowId == flowId)
            .ToListAsync(cancellationToken);
        var conversions = await dbContext
            .CommunicationFlowConversions.AsNoTracking()
            .Where(c => c.FlowId == flowId)
            .ToListAsync(cancellationToken);

        return BuildAnalytics(flow, instanceCount, recipients, conversions);
    }

    private static CommunicationFlowAnalyticsResponse BuildAnalytics(
        CommunicationFlow flow,
        int instanceCount,
        List<CommunicationFlowInstanceRecipient> recipients,
        List<CommunicationFlowConversion> conversions
    )
    {
        var total = recipients.Count;
        var convertedSet = conversions.Select(static c => c.FlowInstanceRecipientId).ToHashSet();
        var totalValue = conversions.Sum(static c => c.ConversionValue ?? 0m);

        var funnel = flow
            .Steps.OrderBy(static s => s.Order)
            .Select(step => new FlowStepPerformanceDto(
                step.Order,
                step.StepKey,
                step.Name,
                step.Channel,
                step.DelayMinutes,
                recipients.Count(r => r.CompletedStepsCount > step.Order),
                conversions.Count(c => c.StepOrderAtConversion == step.Order)
            ))
            .ToList();

        return new CommunicationFlowAnalyticsResponse(
            flow.Id.Value,
            flow.Name,
            flow.Key,
            instanceCount,
            total,
            recipients.Count(static r => r.Status == FlowRecipientStatus.Active),
            recipients.Count(static r => r.Status == FlowRecipientStatus.Completed),
            recipients.Count(static r => r.Status == FlowRecipientStatus.ConvertedExited),
            recipients.Count(static r => r.Status == FlowRecipientStatus.UnsubscribedExited),
            conversions.Count,
            ComputePercent(convertedSet.Count, total),
            totalValue,
            ComputePercent(recipients.Count(static r => r.OpenedStepsCount > 0), total),
            ComputePercent(recipients.Count(static r => r.ClickedStepsCount > 0), total),
            funnel
        );
    }

    private static decimal ComputePercent(int numerator, int denominator)
    {
        return denominator <= 0
            ? 0m
            : Math.Round(numerator * 100m / denominator, 2, MidpointRounding.AwayFromZero);
    }
}
