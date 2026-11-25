using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class StartOidcLoginCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IExternalOidcClient oidcClient,
    IClock clock
) : ICommandHandler<StartOidcLoginCommand, StartOidcLoginResponse>
{
    public async Task<Result<StartOidcLoginResponse>> HandleAsync(
        StartOidcLoginCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var key = command.ProviderKey?.Trim().ToLowerInvariant() ?? string.Empty;
        var provider = await dbContext
            .ExternalAuthProviders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderKey == key && p.IsEnabled, cancellationToken);
        if (provider is null)
        {
            return IdentityErrors.ExternalProviderNotFound;
        }

        var utcNow = clock.UtcNow;
        var state = tokenService.GenerateOpaqueToken(32);
        var nonce = tokenService.GenerateOpaqueToken(24);
        var codeVerifier = tokenService.GenerateOpaqueToken(48);
        var codeChallenge = tokenService.ComputePkceChallenge(codeVerifier);

        var urlResult = await oidcClient.BuildAuthorizationUrlAsync(
            provider,
            state,
            nonce,
            codeChallenge,
            cancellationToken
        );
        if (urlResult.IsFailure)
        {
            return Result.Failure<StartOidcLoginResponse>(urlResult.Errors);
        }

        var session = RemoteAuthenticationSession.Create(
            provider.ProviderKey,
            state,
            nonce,
            codeVerifier,
            command.ReturnUrl,
            utcNow,
            utcNow.ToUniversalTime().Add(AuthenticationPolicy.RemoteAuthSessionLifetime)
        );
        _ = dbContext.RemoteAuthenticationSessions.Add(session);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return new StartOidcLoginResponse(session.Id.Value, urlResult.Value, state);
    }
}
