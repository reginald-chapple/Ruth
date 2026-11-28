using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class UpsertExternalAuthProviderCommandHandler(IdentityDbContext dbContext)
    : ICommandHandler<UpsertExternalAuthProviderCommand, ExternalAuthProviderResponse>
{
    public async Task<Result<ExternalAuthProviderResponse>> HandleAsync(
        UpsertExternalAuthProviderCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var key = command.ProviderKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var existing = await dbContext.ExternalAuthProviders.FirstOrDefaultAsync(
            p => p.ProviderKey == key,
            cancellationToken
        );

        var result = existing is null ? CreateProvider(command) : UpdateProvider(existing, command);
        if (result.IsFailure)
        {
            return Result.Failure<ExternalAuthProviderResponse>(result.Errors);
        }

        var provider = result.Value;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new ExternalAuthProviderResponse(
            provider.Id.Value,
            provider.ProviderKey,
            provider.DisplayName,
            provider.Authority,
            provider.ClientId,
            provider.Scopes,
            provider.IsEnabled,
            provider.AutoLinkByEmail,
            provider.CallbackPath
        );
    }

    private Result<ExternalAuthProviderConfig> CreateProvider(
        UpsertExternalAuthProviderCommand command
    )
    {
        var created = ExternalAuthProviderConfig.Create(
            command.ProviderKey ?? string.Empty,
            command.DisplayName,
            command.Authority,
            command.ClientId,
            command.ClientSecret,
            command.Scopes,
            command.IsEnabled,
            command.AutoLinkByEmail,
            command.CallbackPath
        );
        if (created.IsSuccess)
        {
            _ = dbContext.ExternalAuthProviders.Add(created.Value);
        }

        return created;
    }

    private static Result<ExternalAuthProviderConfig> UpdateProvider(
        ExternalAuthProviderConfig existing,
        UpsertExternalAuthProviderCommand command
    )
    {
        var effectiveSecret = string.Equals(
            command.ClientSecret,
            SensitiveSettingRedactor.RedactedMask,
            StringComparison.Ordinal
        )
            ? null
            : command.ClientSecret;
        var updated = existing.Update(
            command.DisplayName,
            command.Authority,
            command.ClientId,
            effectiveSecret,
            command.Scopes,
            command.IsEnabled,
            command.AutoLinkByEmail,
            command.CallbackPath
        );
        return updated.IsFailure
            ? Result.Failure<ExternalAuthProviderConfig>(updated.Errors)
            : existing;
    }
}
