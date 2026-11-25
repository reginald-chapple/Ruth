using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class GetCheckInConfigurationByIdQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetCheckInConfigurationByIdQuery, CheckInConfigurationDetailResponse>
{
    public async Task<Result<CheckInConfigurationDetailResponse>> HandleAsync(
        GetCheckInConfigurationByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var configId = CheckInConfigurationId.From(query.ConfigurationId);
        var config = await dbContext
            .CheckInConfigurations.AsNoTracking()
            .Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);

        return config is null
            ? Result.Failure<CheckInConfigurationDetailResponse>(
                CheckInErrors.ConfigurationNotFound
            )
            : Result.Success(CheckInConfigurationMapper.ToDetail(config));
    }
}
