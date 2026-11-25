using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetSignatureDocumentsQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetSignatureDocumentsQuery, IReadOnlyList<SignatureDocumentResponse>>
{
    public async Task<Result<IReadOnlyList<SignatureDocumentResponse>>> HandleAsync(
        GetSignatureDocumentsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.SignatureDocuments.AsNoTracking().AsQueryable();
        if (query.TemplateId.HasValue)
        {
            var tid = SignatureDocumentTemplateId.From(query.TemplateId.Value);
            baseQuery = baseQuery.Where(d => d.SignatureDocumentTemplateId == tid);
        }

        if (query.AppliesToEntityId.HasValue)
        {
            var targetId = query.AppliesToEntityId.Value;
            baseQuery = baseQuery.Where(d => d.AppliesToEntityId == targetId);
        }

        var items = await baseQuery
            .OrderByDescending(static d => d.CreatedOnUtc)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SignatureDocumentResponse>>([
            .. items.Select(FileMapper.ToResponse),
        ]);
    }
}
