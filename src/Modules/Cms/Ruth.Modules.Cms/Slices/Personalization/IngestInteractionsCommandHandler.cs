using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class IngestInteractionsCommandHandler(IInteractionBuffer buffer)
    : ICommandHandler<IngestInteractionsCommand, InteractionBufferStatusResponse>
{
    public async Task<Result<InteractionBufferStatusResponse>> HandleAsync(
        IngestInteractionsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<InteractionBufferStatusResponse>(
                CmsErrors.Validation(
                    "Interaction.EmptyBatch",
                    "At least one interaction is required."
                )
            );
        }

        foreach (var item in command.Items)
        {
            await buffer.EnqueueAsync(item, cancellationToken);
        }

        if (command.FlushImmediately)
        {
            var flushed = await buffer.FlushAsync(cancellationToken);
            return Result.Success(flushed with { EnqueuedCount = command.Items.Count });
        }

        return Result.Success(buffer.GetStatus(command.Items.Count));
    }
}
