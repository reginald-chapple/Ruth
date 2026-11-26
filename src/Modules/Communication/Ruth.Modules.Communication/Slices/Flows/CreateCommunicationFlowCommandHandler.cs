using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class CreateCommunicationFlowCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<CreateCommunicationFlowCommand, CommunicationFlowResponse>
{
    public async Task<Result<CommunicationFlowResponse>> HandleAsync(
        CreateCommunicationFlowCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedKey = command.Key?.Trim() ?? string.Empty;
        var exists = await dbContext.CommunicationFlows.AnyAsync(
            f => f.Key == normalizedKey,
            cancellationToken
        );
        if (exists)
        {
            return Result.Failure<CommunicationFlowResponse>(
                CommunicationFlowErrors.FlowKeyDuplicate
            );
        }

        var created = CommunicationFlow.Create(
            new CommunicationFlowSpec(
                command.Name,
                normalizedKey,
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
                IsSystem: false,
                IsActive: command.IsActive
            )
        );
        if (created.IsFailure)
        {
            return Result.Failure<CommunicationFlowResponse>(created.Errors);
        }

        _ = dbContext.CommunicationFlows.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CommunicationFlowMapper.ToResponse(created.Value);
    }
}
