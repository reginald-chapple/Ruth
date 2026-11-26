using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class PreviewIssuedWalletPassQueryHandler(
    CheckInDbContext dbContext,
    WalletPassBundleComposer bundleComposer
) : IQueryHandler<PreviewIssuedWalletPassQuery, WalletPassClaimBundleResponse>
{
    public async Task<Result<WalletPassClaimBundleResponse>> HandleAsync(
        PreviewIssuedWalletPassQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = IssuedWalletPassId.From(query.IssuedPassId);
        var pass = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (pass is null)
        {
            return Result.Failure<WalletPassClaimBundleResponse>(WalletPassErrors.PassNotFound);
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
