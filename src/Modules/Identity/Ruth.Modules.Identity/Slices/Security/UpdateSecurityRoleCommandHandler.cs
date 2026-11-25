using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class UpdateSecurityRoleCommandHandler(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclCache
) : ICommandHandler<UpdateSecurityRoleCommand, SecurityRoleResponse>
{
    public async Task<Result<SecurityRoleResponse>> HandleAsync(
        UpdateSecurityRoleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var roleId = SecurityRoleId.From(command.RoleId);
        var role = await dbContext.SecurityRoles.FirstOrDefaultAsync(
            r => r.Id == roleId,
            cancellationToken
        );
        if (role is null)
        {
            return IdentityErrors.RoleNotFound;
        }

        var updated = role.Update(command.Name, command.Description, command.IsActive);
        if (updated.IsFailure)
        {
            return Result.Failure<SecurityRoleResponse>(updated.Errors);
        }

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
        var memberCount = await dbContext.UserRoles.CountAsync(
            ur => ur.SecurityRoleId == role.Id,
            cancellationToken
        );
        return new SecurityRoleResponse(
            role.Id.Value,
            role.Name,
            role.Description,
            role.IsSystem,
            role.IsActive,
            memberCount,
            role.CreatedOnUtc
        );
    }
}
