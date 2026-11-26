using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class GetPassKitLatestPassQueryHandler(
    CheckInDbContext dbContext,
    WalletPassBundleComposer bundleComposer
) : IQueryHandler<GetPassKitLatestPassQuery, PassKitLatestPassResponse>
{
    public async Task<Result<PassKitLatestPassResponse>> HandleAsync(
        GetPassKitLatestPassQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var passType = query.PassTypeIdentifier?.Trim().ToLowerInvariant() ?? string.Empty;
        var serial = query.SerialNumber?.Trim() ?? string.Empty;
        var pass = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PassTypeIdentifier == passType && p.SerialNumber == serial,
                cancellationToken
            );
        if (pass is null)
        {
            return Result.Failure<PassKitLatestPassResponse>(WalletPassErrors.PassNotFound);
        }

        if (!pass.VerifyAppleAuthToken(query.ApplePassAuthToken))
        {
            return Result.Failure<PassKitLatestPassResponse>(WalletPassErrors.PassKitUnauthorized);
        }

        var lastModified = DateTimeOffset.FromUnixTimeSeconds(
            pass.LastUpdatedAtUtc.ToUnixTimeSeconds()
        );
        if (
            query.IfModifiedSinceUtc.HasValue
            && lastModified <= query.IfModifiedSinceUtc.Value.ToUniversalTime()
        )
        {
            return new PassKitLatestPassResponse(NotModified: true, lastModified, []);
        }

        var template = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == pass.TemplateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure<PassKitLatestPassResponse>(WalletPassErrors.TemplateNotFound);
        }

        var pkpassBytes = await bundleComposer.BuildPassKitArchiveBytesAsync(
            pass,
            template,
            query.ApplePassAuthToken!.Trim(),
            query.PublicBaseUrl,
            cancellationToken
        );
        return new PassKitLatestPassResponse(NotModified: false, lastModified, pkpassBytes);
    }
}
