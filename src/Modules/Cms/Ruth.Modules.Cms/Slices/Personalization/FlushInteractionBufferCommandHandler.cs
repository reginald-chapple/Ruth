using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Infrastructure.Personalization;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Personalization;

internal sealed class FlushInteractionBufferCommandHandler(IInteractionBuffer buffer)
    : ICommandHandler<FlushInteractionBufferCommand, InteractionBufferStatusResponse>
{
    public async Task<Result<InteractionBufferStatusResponse>> HandleAsync(
        FlushInteractionBufferCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var status = await buffer.FlushAsync(cancellationToken);
        return Result.Success(status);
    }
}
