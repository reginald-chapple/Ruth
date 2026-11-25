using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Verification;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Verification;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Verification;

internal sealed class IssueIdentityVerificationCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<IssueIdentityVerificationCommand, IdentityVerificationIssuedResponse>
{
    public async Task<Result<IdentityVerificationIssuedResponse>> HandleAsync(
        IssueIdentityVerificationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (
            string.IsNullOrWhiteSpace(command.ReferenceNumber)
            || string.IsNullOrWhiteSpace(command.RequestIpAddress)
        )
        {
            return CrmErrors.IdentityVerificationReferenceRequired;
        }

        var nowUtc = clock.UtcNow;
        var dayStartUtc = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);
        var ip = command.RequestIpAddress.Trim();
        var limit = Math.Max(1, command.DailyIpLimit);

        var todayCount = await dbContext
            .IdentityVerifications.AsNoTracking()
            .CountAsync(
                v => v.RequestIpAddress == ip && v.IssuedOnUtc >= dayStartUtc,
                cancellationToken
            );
        if (todayCount >= limit)
        {
            return CrmErrors.IdentityVerificationIpLimitReached;
        }

        var codeEntity = await AcquireCodeAsync(nowUtc, dayStartUtc, cancellationToken);
        var verificationResult = IdentityVerification.Create(
            command.ReferenceNumber,
            ip,
            codeEntity.Id,
            nowUtc
        );
        if (verificationResult.IsFailure)
        {
            return Result.Failure<IdentityVerificationIssuedResponse>(verificationResult.Errors);
        }

        _ = dbContext.IdentityVerifications.Add(verificationResult.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new IdentityVerificationIssuedResponse(
                verificationResult.Value.Id.Value,
                verificationResult.Value.ReferenceNumber,
                nowUtc,
                nowUtc.AddMinutes(15),
                codeEntity.Code
            )
        );
    }

    private async Task<IdentityVerificationCode> AcquireCodeAsync(
        DateTimeOffset nowUtc,
        DateTimeOffset dayStartUtc,
        CancellationToken cancellationToken
    )
    {
        var reusable = await dbContext
            .IdentityVerificationCodes.Where(c =>
                !c.LastIssuedOnUtc.HasValue || c.LastIssuedOnUtc.Value < dayStartUtc
            )
            .OrderBy(static c => c.LastIssuedOnUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (reusable is not null)
        {
            reusable.MarkIssued(nowUtc);
            return reusable;
        }

        var codeValue = await GenerateUniqueSixDigitCodeAsync(cancellationToken);
        var created = IdentityVerificationCode.Create(codeValue, nowUtc);
        _ = dbContext.IdentityVerificationCodes.Add(created);
        return created;
    }

    private async Task<string> GenerateUniqueSixDigitCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var number = RandomNumberGenerator.GetInt32(100000, 1000000);
            var candidate = number.ToString("D6", CultureInfo.InvariantCulture);
            var exists = await dbContext
                .IdentityVerificationCodes.AsNoTracking()
                .AnyAsync(c => c.Code == candidate, cancellationToken);
            if (!exists)
            {
                return candidate;
            }
        }

        return RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString("D6", CultureInfo.InvariantCulture);
    }
}
