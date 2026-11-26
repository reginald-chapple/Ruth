using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class ResolveKioskWalletPassScanQueryHandler(
    CheckInDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : IQueryHandler<ResolveKioskWalletPassScanQuery, KioskWalletPassScanResponse>
{
    public async Task<Result<KioskWalletPassScanResponse>> HandleAsync(
        ResolveKioskWalletPassScanQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.ScanToken))
        {
            return Result.Failure<KioskWalletPassScanResponse>(WalletPassErrors.PassNotFound);
        }

        var started = Stopwatch.GetTimestamp();
        var normalizedToken = query.ScanToken.Trim().ToUpperInvariant();
        var pass = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ScanToken == normalizedToken, cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (pass is null)
        {
            return Result.Failure<KioskWalletPassScanResponse>(WalletPassErrors.PassNotFound);
        }

        var activeCheck = pass.EnsureActiveForScan(clock.UtcNow);
        if (activeCheck.IsFailure)
        {
            return Result.Failure<KioskWalletPassScanResponse>(activeCheck.Errors);
        }

        var templateName = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .Where(t => t.Id == pass.TemplateId)
            .Select(static t => t.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Wallet Pass";

        var candidates = await TryEvaluateKioskCandidatesAsync(
            query,
            normalizedToken,
            cancellationToken
        );
        return new KioskWalletPassScanResponse(
            WalletPassMapper.ToResponse(pass, templateName, registeredDeviceCount: 0),
            pass.PersonId,
            Math.Round(elapsedMs, 3, MidpointRounding.AwayFromZero),
            candidates
        );
    }

    private async Task<KioskSearchResponse?> TryEvaluateKioskCandidatesAsync(
        ResolveKioskWalletPassScanQuery query,
        string normalizedToken,
        CancellationToken cancellationToken
    )
    {
        if (
            !query.ConfigurationId.HasValue
            || query.ConfigurationId.Value == Guid.Empty
            || !query.ScheduleId.HasValue
            || query.ScheduleId.Value == Guid.Empty
        )
        {
            return null;
        }

        var searchResult = await queryDispatcher.QueryAsync<
            KioskSearchCandidatesQuery,
            KioskSearchResponse
        >(
            new KioskSearchCandidatesQuery(
                query.ConfigurationId.Value,
                query.ScheduleId.Value,
                normalizedToken,
                KioskSearchInputKind.BarcodeOrCard,
                query.ServiceDate
            ),
            cancellationToken
        );
        return searchResult.IsSuccess ? searchResult.Value : null;
    }
}
