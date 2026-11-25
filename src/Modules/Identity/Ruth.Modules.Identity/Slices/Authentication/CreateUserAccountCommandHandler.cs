using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class CreateUserAccountCommandHandler(
    IdentityDbContext dbContext,
    IPasswordHashingService passwordHashingService,
    IClock clock
) : ICommandHandler<CreateUserAccountCommand, UserAccountSummaryResponse>
{
    public async Task<Result<UserAccountSummaryResponse>> HandleAsync(
        CreateUserAccountCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var passwordValidation = OnboardingPolicy.ValidatePasswordComplexity(command.Password);
        if (passwordValidation.IsFailure)
        {
            return Result.Failure<UserAccountSummaryResponse>(passwordValidation.Errors);
        }

        var utcNow = clock.UtcNow;
        var passwordHash = passwordHashingService.HashPassword(command.Password);
        var accountResult = UserAccount.Create(
            command.UserName,
            command.Email,
            command.DisplayName,
            passwordHash,
            command.IsConfirmed,
            utcNow
        );
        if (accountResult.IsFailure)
        {
            return Result.Failure<UserAccountSummaryResponse>(accountResult.Errors);
        }

        var account = accountResult.Value;
        var roles = await AssignRequestedRolesAsync(account, command.RoleIds, cancellationToken);
        return await PersistAccountAsync(account, roles, cancellationToken);
    }

    private async Task<List<SecurityRole>> AssignRequestedRolesAsync(
        UserAccount account,
        IReadOnlyList<Guid>? roleIds,
        CancellationToken cancellationToken
    )
    {
        var requestedRoleIds = (roleIds ?? [])
            .Where(static id => id != Guid.Empty)
            .Distinct()
            .Select(SecurityRoleId.From)
            .ToList();

        var roles =
            requestedRoleIds.Count == 0
                ? []
                : await dbContext
                    .SecurityRoles.AsNoTracking()
                    .Where(r => requestedRoleIds.Contains(r.Id))
                    .OrderBy(static r => r.Name)
                    .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            _ = account.AddRole(role.Id);
        }

        return roles;
    }

    private async Task<Result<UserAccountSummaryResponse>> PersistAccountAsync(
        UserAccount account,
        List<SecurityRole> roles,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.UserAccounts.Add(account);
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
            return IdentityErrors.UserAlreadyExists;
        }

        return new UserAccountSummaryResponse(
            account.Id.Value,
            account.UserName,
            account.Email,
            account.DisplayName,
            account.IsConfirmed,
            account.IsLockedOut,
            account.IsActive,
            account.LastLoginOnUtc,
            account.CreatedOnUtc,
            [.. roles.Select(static r => r.Name)],
            [.. roles.Select(static r => r.Id.Value)]
        );
    }
}
