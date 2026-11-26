using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class DeleteCommunicationFlowCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteCommunicationFlowCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteCommunicationFlowCommand command,
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
            return Result.Failure<bool>(CommunicationFlowErrors.FlowNotFound);
        }

        var check = flow.EnsureCanDelete();
        if (check.IsFailure)
        {
            return Result.Failure<bool>(check.Errors);
        }

        var hasInstances = await dbContext.CommunicationFlowInstances.AnyAsync(
            i => i.FlowId == id,
            cancellationToken
        );
        if (hasInstances)
        {
            return Result.Failure<bool>(CommunicationFlowErrors.FlowHasInstances);
        }

        _ = dbContext.CommunicationFlows.Remove(flow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
