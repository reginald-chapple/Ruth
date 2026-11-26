using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class DeleteSmsPipelineCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteSmsPipelineCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteSmsPipelineCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = SmsPipelineId.From(command.PipelineId);
        var pipeline = await dbContext.SmsPipelines.FirstOrDefaultAsync(
            p => p.Id == id,
            cancellationToken
        );
        if (pipeline is null)
        {
            return Result.Failure<bool>(BulkCommunicationErrors.SmsPipelineNotFound);
        }

        var check = pipeline.EnsureCanDelete();
        if (check.IsFailure)
        {
            return Result.Failure<bool>(check.Errors);
        }

        _ = dbContext.SmsPipelines.Remove(pipeline);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
