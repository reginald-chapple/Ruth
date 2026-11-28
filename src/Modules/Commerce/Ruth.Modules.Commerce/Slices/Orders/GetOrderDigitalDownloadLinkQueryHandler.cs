using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Orders;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Orders;

internal sealed class GetOrderDigitalDownloadLinkQueryHandler(
    CommerceDbContext dbContext,
    IQueryDispatcher queryDispatcher
) : IQueryHandler<GetOrderDigitalDownloadLinkQuery, OrderDigitalDownloadLinkResponse>
{
    public async Task<Result<OrderDigitalDownloadLinkResponse>> HandleAsync(
        GetOrderDigitalDownloadLinkQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var order = await LoadOrderAsync(dbContext, query.OrderNumberOrId, cancellationToken);
        if (order is null)
        {
            return CommerceErrors.OrderNotFound;
        }

        var line = order.Lines.FirstOrDefault(l => l.Id.Value == query.LineId);
        if (
            !order.IsEligibleForDigitalDownload()
            || line is null
            || line.ProductKind != ProductKind.DigitalDownload
            || line.DigitalBinaryFileId is not { } binaryFileId
        )
        {
            return CommerceErrors.DigitalDownloadNotEligible;
        }

        var minutes = Math.Clamp(query.ExpiresInMinutes, 1, 15);
        var urlRes = await queryDispatcher.QueryAsync<
            GetBinaryFilePresignedUrlQuery,
            StoragePresignedUrl
        >(new GetBinaryFilePresignedUrlQuery(binaryFileId, minutes), cancellationToken);

        return urlRes.IsFailure
            ? Result.Failure<OrderDigitalDownloadLinkResponse>(urlRes.Errors)
            : new OrderDigitalDownloadLinkResponse(
                order.Id.Value,
                line.Id.Value,
                binaryFileId,
                urlRes.Value.Url.ToString(),
                urlRes.Value.ExpiresAtUtc
            );
    }

    internal static async Task<CommerceOrder?> LoadOrderAsync(
        CommerceDbContext dbContext,
        string numberOrId,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(numberOrId))
        {
            return null;
        }

        var trimmed = numberOrId.Trim();
        var upperNumber = trimmed.ToUpperInvariant();
        var q = dbContext.Orders.AsNoTracking().Include(static o => o.Lines);
        return Guid.TryParse(trimmed, out var guid)
            ? await q.FirstOrDefaultAsync(
                o => o.Id == CommerceOrderId.From(guid),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(o => o.OrderNumber == upperNumber, cancellationToken);
    }
}
