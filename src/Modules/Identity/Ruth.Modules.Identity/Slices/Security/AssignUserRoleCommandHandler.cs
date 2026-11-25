using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class AssignUserRoleCommandHandler(IdentityDbContext dbContext)
    : ICommandHandler<AssignUserRoleCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        AssignUserRoleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var userId = UserAccountId.From(command.UserAccountId);
        var roleId = SecurityRoleId.From(command.SecurityRoleId);

        var account = await dbContext
            .UserAccounts.Include(static u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (account is null)
        {
            return IdentityErrors.UserAccountNotFound;
        }

        var roleExists = await dbContext.SecurityRoles.AnyAsync(
            r => r.Id == roleId,
            cancellationToken
        );
        if (!roleExists)
        {
            return IdentityErrors.RoleNotFound;
        }

        var addResult = account.AddRole(roleId);
        if (addResult.IsFailure)
        {
            return Result.Failure<bool>(addResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
