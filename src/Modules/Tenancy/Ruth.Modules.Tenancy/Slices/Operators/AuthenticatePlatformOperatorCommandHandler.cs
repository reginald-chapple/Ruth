using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.Modules.Tenancy.Infrastructure.Security;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Tenancy.Slices.Operators;

internal sealed class AuthenticatePlatformOperatorCommandHandler(TenancyDbContext dbContext)
    : ICommandHandler<AuthenticatePlatformOperatorCommand, PlatformOperatorAuthResponse>
{
    private readonly TenancyDbContext _dbContext = dbContext
        ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Result<PlatformOperatorAuthResponse>> HandleAsync(
        AuthenticatePlatformOperatorCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
        {
            return TenancyErrors.InvalidOperatorCredentials;
        }

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var op = await _dbContext
            .PlatformOperators.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.EmailNormalized == normalizedEmail && x.IsActive,
                cancellationToken
            );

        return
            op is not null
            && PlatformOperatorPasswordHasher.VerifyPassword(command.Password, op.PasswordHash)
            ? new PlatformOperatorAuthResponse(op.Id.Value, op.EmailNormalized, op.DisplayName)
            : TenancyErrors.InvalidOperatorCredentials;
    }
}
