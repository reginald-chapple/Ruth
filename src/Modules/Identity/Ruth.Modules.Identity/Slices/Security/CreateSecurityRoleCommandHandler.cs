using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class CreateSecurityRoleCommandHandler(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclCache
) : ICommandHandler<CreateSecurityRoleCommand, SecurityRoleResponse>
{
    public async Task<Result<SecurityRoleResponse>> HandleAsync(
        CreateSecurityRoleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = SecurityRole.Create(
            SecurityRoleId.Create(),
            command.Name,
            isSystem: false,
            command.Description
        );
        if (created.IsFailure)
        {
            return Result.Failure<SecurityRoleResponse>(created.Errors);
        }

        var role = created.Value;
        _ = dbContext.SecurityRoles.Add(role);
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
            return IdentityErrors.RoleAlreadyExists;
        }

        await aclCache.InvalidateAsync(cancellationToken);
        return new SecurityRoleResponse(
            role.Id.Value,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsActive,
            MemberCount: 0,
            role.CreatedOnUtc
        );
    }
}
