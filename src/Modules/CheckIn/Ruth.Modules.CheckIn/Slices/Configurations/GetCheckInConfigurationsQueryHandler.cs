using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class GetCheckInConfigurationsQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<
        GetCheckInConfigurationsQuery,
        IReadOnlyList<CheckInConfigurationSummaryResponse>
    >
{
    public async Task<Result<IReadOnlyList<CheckInConfigurationSummaryResponse>>> HandleAsync(
        GetCheckInConfigurationsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var configurations = dbContext.CheckInConfigurations.AsNoTracking();
        if (query.BranchId.HasValue)
        {
            configurations = configurations.Where(c =>
                c.BranchId == null || c.BranchId == query.BranchId.Value
            );
        }

        if (query.ActiveOnly)
        {
            configurations = configurations.Where(static c => c.IsActive);
        }

        var items = await configurations
            .OrderBy(static c => c.Name)
            .Select(static c => new CheckInConfigurationSummaryResponse(
                c.Id.Value,
                c.Name,
                c.Description,
                c.ProfileKind,
                c.BranchId,
                c.SearchMode,
                c.IsActive,
                c.Areas.Count,
                c.CreatedOnUtc,
                c.ModifiedOnUtc
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<CheckInConfigurationSummaryResponse>>(items);
    }
}
