using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class CreateAuthClientCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService
) : ICommandHandler<CreateAuthClientCommand, AuthClientResponse>
{
    public async Task<Result<AuthClientResponse>> HandleAsync(
        CreateAuthClientCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var secretHash = string.IsNullOrWhiteSpace(command.ClientSecret)
            ? null
            : tokenService.ComputeSha256Hash(command.ClientSecret);

        var created = AuthClient.Create(
            command.Name,
            command.ClientId,
            secretHash,
            command.RedirectUri,
            command.PostLogoutRedirectUri,
            command.AllowedScopes,
            command.AllowedClaims,
            command.AllowUserApiAccess,
            command.ScopeApprovalExpirationDays
        );
        if (created.IsFailure)
        {
            return Result.Failure<AuthClientResponse>(created.Errors);
        }

        var client = created.Value;
        _ = dbContext.AuthClients.Add(client);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                && string.Equals(
                    pg.SqlState,
                    PostgresErrorCodes.UniqueViolation,
                    StringComparison.Ordinal
                )
            )
        {
            dbContext.ChangeTracker.Clear();
            return IdentityErrors.AuthClientAlreadyExists;
        }

        return new AuthClientResponse(
            client.Id.Value,
            client.Name,
            client.ClientId,
            client.RedirectUri,
            client.PostLogoutRedirectUri,
            client.AllowedScopes,
            client.AllowedClaims,
            client.AllowUserApiAccess,
            client.ScopeApprovalExpirationDays,
            client.IsActive
        );
    }
}
