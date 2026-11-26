using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class GetCommunicationByIdQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationByIdQuery, CommunicationDetailResponse>
{
    public async Task<Result<CommunicationDetailResponse>> HandleAsync(
        GetCommunicationByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = CommunicationId.From(query.CommunicationId);
        var comm = await dbContext
            .Communications.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (comm is null)
        {
            return Result.Failure<CommunicationDetailResponse>(
                BulkCommunicationErrors.CommunicationNotFound
            );
        }

        var recipients = await dbContext
            .CommunicationRecipients.AsNoTracking()
            .Where(r => r.CommunicationId == id)
            .OrderBy(static r => r.DestinationAddress)
            .ToListAsync(cancellationToken);

        return BulkCommunicationMapper.ToDetailResponse(comm, recipients);
    }
}
