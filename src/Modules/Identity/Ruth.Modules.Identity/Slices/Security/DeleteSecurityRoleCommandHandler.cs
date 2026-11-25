using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.Modules.Identity.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class DeleteSecurityRoleCommandHandler(
    IdentityDbContext dbContext,
    IAclSnapshotCache aclCache
) : ICommandHandler<DeleteSecurityRoleCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteSecurityRoleCommand command,
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

        if (role.IsSystem)
        {
            return IdentityErrors.SystemRoleProtected;
        }

        _ = dbContext.SecurityRoles.Remove(role);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await aclCache.InvalidateAsync(cancellationToken);
        return true;
    }
}
