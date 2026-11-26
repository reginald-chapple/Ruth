using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class UpdateSmsPipelineCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<UpdateSmsPipelineCommand, SmsPipelineResponse>
{
    public async Task<Result<SmsPipelineResponse>> HandleAsync(
        UpdateSmsPipelineCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = SmsPipelineId.From(command.PipelineId);
        var pipeline = await dbContext
            .SmsPipelines.Include(static p => p.Actions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pipeline is null)
        {
            return Result.Failure<SmsPipelineResponse>(BulkCommunicationErrors.SmsPipelineNotFound);
        }

        var updated = pipeline.Update(
            command.Name,
            command.Actions,
            command.Description,
            command.SystemPhoneNumberId,
            command.PriorityOrder,
            command.IsActive
        );
        if (updated.IsFailure)
        {
            return Result.Failure<SmsPipelineResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return TwoWaySmsMapper.ToResponse(pipeline);
    }
}
