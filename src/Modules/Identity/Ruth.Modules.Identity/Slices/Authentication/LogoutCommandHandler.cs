using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Domain;
using Ruth.Modules.Identity.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Identity.Slices.Authentication;

internal sealed class LogoutCommandHandler(
    IdentityDbContext dbContext,
    ITokenService tokenService,
    IClock clock
) : ICommandHandler<LogoutCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return true;
        }

        var hash = tokenService.ComputeSha256Hash(command.RefreshToken);
        var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(
            t => t.TokenHash == hash,
            cancellationToken
        );

        if (token is not null && !token.IsRevoked)
        {
            token.Revoke(clock.UtcNow, "UserLogout");
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
