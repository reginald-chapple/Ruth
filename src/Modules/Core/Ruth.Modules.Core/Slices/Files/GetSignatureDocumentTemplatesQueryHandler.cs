using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Files;

internal sealed class GetSignatureDocumentTemplatesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<
        GetSignatureDocumentTemplatesQuery,
        IReadOnlyList<SignatureDocumentTemplateResponse>
    >
{
    public async Task<Result<IReadOnlyList<SignatureDocumentTemplateResponse>>> HandleAsync(
        GetSignatureDocumentTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.SignatureDocumentTemplates.AsNoTracking().AsQueryable();
        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static t => t.IsActive);
        }

        var items = await baseQuery.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<SignatureDocumentTemplateResponse>>([
            .. items.Select(FileMapper.ToResponse),
        ]);
    }
}
