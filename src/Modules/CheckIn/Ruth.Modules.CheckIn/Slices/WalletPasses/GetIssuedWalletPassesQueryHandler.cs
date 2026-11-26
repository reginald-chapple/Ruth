using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class GetIssuedWalletPassesQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetIssuedWalletPassesQuery, IReadOnlyList<IssuedWalletPassResponse>>
{
    public async Task<Result<IReadOnlyList<IssuedWalletPassResponse>>> HandleAsync(
        GetIssuedWalletPassesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.IssuedWalletPasses.AsNoTracking();
        if (query.PersonId.HasValue && query.PersonId.Value != Guid.Empty)
        {
            q = q.Where(p => p.PersonId == query.PersonId.Value);
        }

        if (query.TemplateId.HasValue && query.TemplateId.Value != Guid.Empty)
        {
            var tid = WalletPassTemplateId.From(query.TemplateId.Value);
            q = q.Where(p => p.TemplateId == tid);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static p => p.Status == IssuedWalletPassStatus.Active);
        }

        var passes = await q.OrderByDescending(static p => p.IssuedAtUtc)
            .Take(250)
            .ToListAsync(cancellationToken);
        if (passes.Count == 0)
        {
            return Result.Success<IReadOnlyList<IssuedWalletPassResponse>>([]);
        }

        var templateIds = passes.Select(static p => p.TemplateId).Distinct().ToArray();
        var templates = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        var passIds = passes.Select(static p => p.Id).ToArray();
        var deviceCounts = await dbContext
            .WalletPassDeviceRegistrations.AsNoTracking()
            .Where(r => passIds.Contains(r.IssuedPassId))
            .GroupBy(static r => r.IssuedPassId)
            .Select(static g => new { PassId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.PassId, static x => x.Count, cancellationToken);

        return Result.Success<IReadOnlyList<IssuedWalletPassResponse>>([
            .. passes.Select(p =>
                WalletPassMapper.ToResponse(
                    p,
                    templates.GetValueOrDefault(p.TemplateId, "Wallet Pass"),
                    deviceCounts.GetValueOrDefault(p.Id, 0)
                )
            ),
        ]);
    }
}
