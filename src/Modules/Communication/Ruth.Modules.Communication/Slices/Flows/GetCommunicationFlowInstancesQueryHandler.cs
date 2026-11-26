using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class GetCommunicationFlowInstancesQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<
        GetCommunicationFlowInstancesQuery,
        IReadOnlyList<CommunicationFlowInstanceResponse>
    >
{
    public async Task<Result<IReadOnlyList<CommunicationFlowInstanceResponse>>> HandleAsync(
        GetCommunicationFlowInstancesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var flowId = CommunicationFlowId.From(query.FlowId);
        var instances = await dbContext
            .CommunicationFlowInstances.AsNoTracking()
            .Where(i => i.FlowId == flowId)
            .OrderByDescending(static i => i.StartDateUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (instances.Count == 0)
        {
            return Result.Success<IReadOnlyList<CommunicationFlowInstanceResponse>>([]);
        }

        var instIds = instances.Select(static i => i.Id).ToArray();
        var recipients = await dbContext
            .CommunicationFlowInstanceRecipients.AsNoTracking()
            .Where(r => instIds.Contains(r.FlowInstanceId))
            .OrderBy(static r => r.EnrolledAtUtc)
            .ToListAsync(cancellationToken);

        var byInst = recipients
            .GroupBy(static r => r.FlowInstanceId)
            .ToDictionary(
                static g => g.Key,
                static g => (IReadOnlyList<CommunicationFlowInstanceRecipient>)[.. g]
            );

        var list = instances
            .Select(i =>
                CommunicationFlowMapper.ToResponse(i, byInst.GetValueOrDefault(i.Id) ?? [])
            )
            .ToList();
        return Result.Success<IReadOnlyList<CommunicationFlowInstanceResponse>>(list);
    }
}
