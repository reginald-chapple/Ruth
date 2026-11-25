using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Verification;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Verification;

internal sealed class VerifyIdentityCodeCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<VerifyIdentityCodeCommand, IdentityVerificationResultResponse>
{
    public async Task<Result<IdentityVerificationResultResponse>> HandleAsync(
        VerifyIdentityCodeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = IdentityVerificationId.From(command.VerificationId);
        var verification = await dbContext.IdentityVerifications.FirstOrDefaultAsync(
            v => v.Id == id,
            cancellationToken
        );
        if (verification is null)
        {
            return CrmErrors.IdentityVerificationNotFound;
        }

        var codeEntity = await dbContext
            .IdentityVerificationCodes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == verification.VerificationCodeId, cancellationToken);
        if (codeEntity is null)
        {
            return CrmErrors.IdentityVerificationNotFound;
        }

        var outcome = verification.VerifyCode(
            command.Code,
            codeEntity.Code,
            command.TimeLimitMinutes,
            command.MaxFailedAttempts,
            clock.UtcNow
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return outcome.IsFailure
            ? Result.Failure<IdentityVerificationResultResponse>(outcome.Errors)
            : Result.Success(
                new IdentityVerificationResultResponse(
                    verification.Id.Value,
                    verification.IsVerified,
                    verification.FailedMatchAttemptCount,
                    verification.VerifiedOnUtc
                )
            );
    }
}
