using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.BackgroundChecks;

internal sealed class ProcessBackgroundCheckWebhookCommandHandler(
    CrmDbContext dbContext,
    IBackgroundCheckProviderResolver providerResolver,
    IClock clock
) : ICommandHandler<ProcessBackgroundCheckWebhookCommand, BackgroundCheckResponse>
{
    public async Task<Result<BackgroundCheckResponse>> HandleAsync(
        ProcessBackgroundCheckWebhookCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var provider = providerResolver.Resolve(command.ProviderKey);
        if (provider is null)
        {
            return CrmErrors.BackgroundCheckProviderNotFound;
        }

        if (!provider.VerifyWebhookSignature(command.RawPayload, command.SignatureHeader))
        {
            return CrmErrors.BackgroundCheckWebhookInvalidSignature;
        }

        var parsed = provider.ParseWebhookEvent(command.RawPayload);
        if (parsed.IsFailure)
        {
            return Result.Failure<BackgroundCheckResponse>(parsed.Errors);
        }

        var evt = parsed.Value;
        var providerKey = provider.ProviderKey;
        var check = await dbContext.BackgroundChecks.FirstOrDefaultAsync(
            b => b.ProviderKey == providerKey && b.RequestId == evt.ProviderRequestId,
            cancellationToken
        );
        if (check is null)
        {
            return CrmErrors.BackgroundCheckNotFound;
        }

        check.ApplyWebhookEvent(evt, clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(RequestBackgroundCheckCommandHandler.ToResponse(check));
    }
}
