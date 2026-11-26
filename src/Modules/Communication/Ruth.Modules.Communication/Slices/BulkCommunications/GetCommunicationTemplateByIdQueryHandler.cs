using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class GetCommunicationTemplateByIdQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetCommunicationTemplateByIdQuery, CommunicationTemplateResponse>
{
    public async Task<Result<CommunicationTemplateResponse>> HandleAsync(
        GetCommunicationTemplateByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var id = CommunicationTemplateId.From(query.TemplateId);
        var template = await dbContext
            .CommunicationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return template is null
            ? Result.Failure<CommunicationTemplateResponse>(
                BulkCommunicationErrors.TemplateNotFound
            )
            : BulkCommunicationMapper.ToResponse(template);
    }
}
