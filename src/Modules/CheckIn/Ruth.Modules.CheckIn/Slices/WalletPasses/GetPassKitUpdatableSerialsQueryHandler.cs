using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class GetPassKitUpdatableSerialsQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetPassKitUpdatableSerialsQuery, PassKitSerialNumbersResponse>
{
    public async Task<Result<PassKitSerialNumbersResponse>> HandleAsync(
        GetPassKitUpdatableSerialsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var deviceId = query.DeviceLibraryIdentifier?.Trim() ?? string.Empty;
        var passType = query.PassTypeIdentifier?.Trim().ToLowerInvariant() ?? string.Empty;

        var registeredPassIds = await dbContext
            .WalletPassDeviceRegistrations.AsNoTracking()
            .Where(r => r.DeviceLibraryIdentifier == deviceId && r.PassTypeIdentifier == passType)
            .Select(static r => r.IssuedPassId)
            .ToArrayAsync(cancellationToken);
        if (registeredPassIds.Length == 0)
        {
            return new PassKitSerialNumbersResponse([], "0");
        }

        var sinceSeq =
            !string.IsNullOrWhiteSpace(query.PassesUpdatedSinceTag)
            && long.TryParse(
                query.PassesUpdatedSinceTag.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed
            )
                ? parsed
                : 0L;

        var updatedPasses = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .Where(p => registeredPassIds.Contains(p.Id) && p.UpdatedSequence > sinceSeq)
            .OrderBy(static p => p.UpdatedSequence)
            .Select(static p => new { p.SerialNumber, p.UpdatedSequence })
            .ToListAsync(cancellationToken);

        if (updatedPasses.Count == 0)
        {
            return new PassKitSerialNumbersResponse(
                [],
                sinceSeq.ToString(CultureInfo.InvariantCulture)
            );
        }

        var maxSeq = updatedPasses[^1].UpdatedSequence;
        return new PassKitSerialNumbersResponse(
            [.. updatedPasses.Select(static p => p.SerialNumber)],
            maxSeq.ToString(CultureInfo.InvariantCulture)
        );
    }
}
