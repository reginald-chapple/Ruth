using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class CreateAuthScopeCommandHandler(IdentityDbContext dbContext)
    : ICommandHandler<CreateAuthScopeCommand, AuthScopeResponse>
{
    public async Task<Result<AuthScopeResponse>> HandleAsync(
        CreateAuthScopeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = AuthScope.Create(command.Name, command.PublicName);
        if (created.IsFailure)
        {
            return Result.Failure<AuthScopeResponse>(created.Errors);
        }

        var scope = created.Value;
        _ = dbContext.AuthScopes.Add(scope);
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
            return IdentityErrors.AuthScopeAlreadyExists;
        }

        return new AuthScopeResponse(
            scope.Id.Value,
            scope.Name,
            scope.PublicName,
            scope.IsSystem,
            scope.IsActive
        );
    }
}
