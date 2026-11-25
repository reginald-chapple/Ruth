using System.Net;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class MatchDeviceQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<MatchDeviceQuery, DeviceResponse>
{
    public async Task<Result<DeviceResponse>> HandleAsync(
        MatchDeviceQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (query.DeviceTypeValueId == Guid.Empty)
        {
            return CoreLocationErrors.DeviceTypeRequired;
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            var matchedByIp = await MatchByIpOrHostAsync(query, cancellationToken);
            if (matchedByIp is not null)
            {
                return Result.Success(DeviceMapper.ToResponse(matchedByIp));
            }
        }

        if (query.Latitude.HasValue && query.Longitude.HasValue)
        {
            var geoResult = await MatchByGeoFenceAsync(query, cancellationToken);
            if (geoResult.IsFailure || geoResult.Value is not null)
            {
                return geoResult.IsFailure
                    ? Result.Failure<DeviceResponse>(geoResult.Errors)
                    : Result.Success(DeviceMapper.ToResponse(geoResult.Value!));
            }
        }

        return CoreLocationErrors.DeviceNotFound;
    }

    private async Task<Device?> MatchByIpOrHostAsync(
        MatchDeviceQuery query,
        CancellationToken cancellationToken
    )
    {
        var normalizedIp = query.IpAddress!.Trim().ToUpperInvariant();
        var typeId = query.DeviceTypeValueId;

        var direct = await dbContext
            .Devices.AsNoTracking()
            .Include(static d => d.Locations)
            .FirstOrDefaultAsync(
                d =>
                    d.IsActive
                    && d.DeviceTypeValueId == typeId
                    && d.NormalizedIpAddress == normalizedIp,
                cancellationToken
            );
        if (direct is not null || query.SkipReverseDns)
        {
            return direct;
        }

        var hostName = await TryResolveHostNameAsync(query.IpAddress!, cancellationToken);
        if (string.IsNullOrWhiteSpace(hostName))
        {
            return null;
        }

        var normalizedHost = hostName.Trim().ToUpperInvariant();
        var byHostIp = await dbContext
            .Devices.AsNoTracking()
            .Include(static d => d.Locations)
            .FirstOrDefaultAsync(
                d =>
                    d.IsActive
                    && d.DeviceTypeValueId == typeId
                    && d.NormalizedIpAddress == normalizedHost,
                cancellationToken
            );
        return byHostIp is not null
            ? byHostIp
            : await dbContext
                .Devices.AsNoTracking()
                .Include(static d => d.Locations)
                .FirstOrDefaultAsync(
                    d =>
                        d.IsActive
                        && d.DeviceTypeValueId == typeId
                        && d.NormalizedName == normalizedHost,
                    cancellationToken
                );
    }

    private async Task<Result<Device?>> MatchByGeoFenceAsync(
        MatchDeviceQuery query,
        CancellationToken cancellationToken
    )
    {
        var pointResult = GeoGeometryHelper.CreatePoint(
            new GeoPointDto(query.Latitude!.Value, query.Longitude!.Value)
        );
        if (pointResult.IsFailure || pointResult.Value is null)
        {
            return Result.Failure<Device?>(pointResult.Errors);
        }

        var point = pointResult.Value;
        var typeId = query.DeviceTypeValueId;

        var matchingLocationIds = await dbContext
            .Locations.AsNoTracking()
            .Where(l => l.IsActive && l.GeoFence != null && l.GeoFence.Covers(point))
            .Select(static l => l.Id)
            .ToListAsync(cancellationToken);

        if (matchingLocationIds.Count == 0)
        {
            return Result.Success<Device?>(value: null);
        }

        var matched = await dbContext
            .Devices.AsNoTracking()
            .Include(static d => d.Locations)
            .Where(d =>
                d.IsActive
                && d.DeviceTypeValueId == typeId
                && d.LocationId != null
                && matchingLocationIds.Contains(d.LocationId.Value)
            )
            .OrderBy(static d => d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return Result.Success(matched);
    }

    private static async Task<string?> TryResolveHostNameAsync(
        string ipAddress,
        CancellationToken cancellationToken
    )
    {
        if (!IPAddress.TryParse(ipAddress.Trim(), out var parsed))
        {
            return null;
        }

        try
        {
            var entry = await Dns.GetHostEntryAsync(parsed).WaitAsync(cancellationToken);
            return entry.HostName;
        }
        catch (SocketException)
        {
            return null;
        }
    }
}
