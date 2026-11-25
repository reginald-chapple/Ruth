using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Branches;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Branches;

internal sealed class GetBranchesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetBranchesQuery, IReadOnlyList<BranchResponse>>
{
    public async Task<Result<IReadOnlyList<BranchResponse>>> HandleAsync(
        GetBranchesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .Branches.AsNoTracking()
            .Include(static b => b.Schedules)
            .Include(static b => b.Topics)
            .AsQueryable();

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static b => b.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(b =>
                b.NormalizedName.Contains(term)
                || (
                    b.ShortCode != null
                    && b.ShortCode.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                )
            );
        }

        var branches = await baseQuery
            .OrderBy(static b => b.Order)
            .ThenBy(static b => b.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<BranchResponse>>([
            .. branches.Select(BranchMapper.ToResponse),
        ]);
    }
}
