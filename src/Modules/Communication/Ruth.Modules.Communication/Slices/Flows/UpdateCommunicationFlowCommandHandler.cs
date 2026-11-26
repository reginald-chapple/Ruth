using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class UpdateCommunicationFlowCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<UpdateCommunicationFlowCommand, CommunicationFlowResponse>
{
    public async Task<Result<CommunicationFlowResponse>> HandleAsync(
        UpdateCommunicationFlowCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CommunicationFlowId.From(command.FlowId);
        var flow = await dbContext.CommunicationFlows.FirstOrDefaultAsync(
            f => f.Id == id,
            cancellationToken
        );
        if (flow is null)
        {
            return Result.Failure<CommunicationFlowResponse>(CommunicationFlowErrors.FlowNotFound);
        }

        var updated = flow.Update(
            new CommunicationFlowSpec(
                command.Name,
                flow.Key,
                command.Category,
                command.Steps,
                command.TopicKey,
                command.Description,
                command.TriggerMode,
                command.ExitCondition,
                command.ConversionGoalKind,
                command.ConversionTargetGroupId,
                command.ConversionGoalUrlPrefix,
                command.ConversionWindowDays,
                flow.IsSystem,
                command.IsActive
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CommunicationFlowResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CommunicationFlowMapper.ToResponse(flow);
    }
}
