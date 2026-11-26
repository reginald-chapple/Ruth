using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class GetWalletPassTemplatesQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetWalletPassTemplatesQuery, IReadOnlyList<WalletPassTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<WalletPassTemplateResponse>>> HandleAsync(
        GetWalletPassTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.WalletPassTemplates.AsNoTracking();
        if (query.ActiveOnly)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<WalletPassTemplateResponse>>([
            .. items.Select(WalletPassMapper.ToResponse),
        ]);
    }
}
