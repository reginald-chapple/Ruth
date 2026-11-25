using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class RemoveCheckInAreaCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<RemoveCheckInAreaCommand, CheckInConfigurationDetailResponse>
{
    public async Task<Result<CheckInConfigurationDetailResponse>> HandleAsync(
        RemoveCheckInAreaCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var configId = CheckInConfigurationId.From(command.ConfigurationId);
        var config = await dbContext
            .CheckInConfigurations.Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .FirstOrDefaultAsync(c => c.Id == configId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(
                CheckInErrors.ConfigurationNotFound
            );
        }

        var removed = config.RemoveArea(CheckInAreaId.From(command.AreaId));
        if (removed.IsFailure)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(removed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInConfigurationMapper.ToDetail(config));
    }
}
