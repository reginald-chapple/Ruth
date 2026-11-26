using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class GetSystemCommunicationByIdQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetSystemCommunicationByIdQuery, SystemCommunicationResponse>
{
    public async Task<Result<SystemCommunicationResponse>> HandleAsync(
        GetSystemCommunicationByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = SystemCommunicationId.From(query.SystemCommunicationId);
        var entity = await dbContext
            .SystemCommunications.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is null
            ? CommunicationDomainErrors.SystemCommunicationNotFound
            : SystemCommunicationMapper.ToResponse(entity);
    }
}
