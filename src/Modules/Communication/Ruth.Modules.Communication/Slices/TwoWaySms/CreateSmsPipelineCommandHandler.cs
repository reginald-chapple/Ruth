using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class CreateSmsPipelineCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<CreateSmsPipelineCommand, SmsPipelineResponse>
{
    public async Task<Result<SmsPipelineResponse>> HandleAsync(
        CreateSmsPipelineCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = SmsPipeline.Create(
            command.Name,
            command.Actions,
            command.Description,
            command.SystemPhoneNumberId,
            command.PriorityOrder,
            command.IsActive
        );
        if (created.IsFailure)
        {
            return Result.Failure<SmsPipelineResponse>(created.Errors);
        }

        _ = dbContext.SmsPipelines.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return TwoWaySmsMapper.ToResponse(created.Value);
    }
}
