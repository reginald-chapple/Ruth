using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class GetCommunicationFlowByIdQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationFlowByIdQuery, CommunicationFlowResponse>
{
    public async Task<Result<CommunicationFlowResponse>> HandleAsync(
        GetCommunicationFlowByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = CommunicationFlowId.From(query.FlowId);
        var flow = await dbContext
            .CommunicationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return flow is null
            ? Result.Failure<CommunicationFlowResponse>(CommunicationFlowErrors.FlowNotFound)
            : CommunicationFlowMapper.ToResponse(flow);
    }
}
