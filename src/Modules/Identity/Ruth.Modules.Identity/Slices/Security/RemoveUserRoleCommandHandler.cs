using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Security;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Identity.Slices.Security;

internal sealed class RemoveUserRoleCommandHandler(IdentityDbContext dbContext)
    : ICommandHandler<RemoveUserRoleCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RemoveUserRoleCommand command,
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

        var removeResult = account.RemoveRole(roleId);
        if (removeResult.IsFailure)
        {
            return Result.Failure<bool>(removeResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
