using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class ClaimWalletPassQueryHandler(
    CheckInDbContext dbContext,
    WalletPassBundleComposer bundleComposer
) : IQueryHandler<ClaimWalletPassQuery, WalletPassClaimBundleResponse>
{
    public async Task<Result<WalletPassClaimBundleResponse>> HandleAsync(
        ClaimWalletPassQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.ClaimToken))
        {
            return Result.Failure<WalletPassClaimBundleResponse>(
                WalletPassErrors.ClaimTokenInvalid
            );
        }

        var claimHash = WalletPassSecurityTokens.ComputeSha256Hex(query.ClaimToken);
        var pass = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ClaimTokenHash == claimHash, cancellationToken);
        if (
            pass is null
            || !WalletPassSecurityTokens.ConstantTimeMatchesHash(
                query.ClaimToken,
                pass.ClaimTokenHash
            )
        )
        {
            return Result.Failure<WalletPassClaimBundleResponse>(
                WalletPassErrors.ClaimTokenInvalid
            );
        }

        var template = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == pass.TemplateId, cancellationToken);
        if (template is null)
        {
            return Result.Failure<WalletPassClaimBundleResponse>(WalletPassErrors.TemplateNotFound);
        }

        var deviceCount = await dbContext.WalletPassDeviceRegistrations.CountAsync(
            r => r.IssuedPassId == pass.Id,
            cancellationToken
        );

        return await bundleComposer.BuildClaimBundleAsync(
            pass,
            template,
            deviceCount,
            query.PublicBaseUrl,
            cancellationToken
        );
    }
}
