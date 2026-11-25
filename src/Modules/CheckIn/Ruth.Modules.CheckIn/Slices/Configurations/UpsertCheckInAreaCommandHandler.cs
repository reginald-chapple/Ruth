using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class UpsertCheckInAreaCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<UpsertCheckInAreaCommand, CheckInConfigurationDetailResponse>
{
    public async Task<Result<CheckInConfigurationDetailResponse>> HandleAsync(
        UpsertCheckInAreaCommand command,
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

        var priorAreaIds = config.Areas.Select(static a => a.Id).ToHashSet();
        var priorRoomIds = config
            .Areas.SelectMany(static a => a.Rooms)
            .Select(static r => r.Id)
            .ToHashSet();

        var areaId = command.AreaId.HasValue
            ? CheckInAreaId.From(command.AreaId.Value)
            : (CheckInAreaId?)null;
        var upsert = config.UpsertArea(
            new CheckInAreaSpec(
                command.Name,
                command.Description,
                command.LinkedGroupId,
                command.BalancingStrategy,
                command.Order,
                command.IsActive,
                command.PrintBadge,
                command.IssueSecurityPickupCode,
                command.Eligibility,
                command.Rooms
            ),
            areaId
        );
        if (upsert.IsFailure)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(upsert.Errors);
        }

        MarkNewChildrenAsAdded(dbContext, config, priorAreaIds, priorRoomIds);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInConfigurationMapper.ToDetail(config));
    }

    private static void MarkNewChildrenAsAdded(
        CheckInDbContext dbContext,
        CheckInConfiguration config,
        HashSet<CheckInAreaId> priorAreaIds,
        HashSet<CheckInAreaLocationId> priorRoomIds
    )
    {
        foreach (var area in config.Areas)
        {
            if (!priorAreaIds.Contains(area.Id))
            {
                dbContext.Entry(area).State = EntityState.Added;
            }

            foreach (var room in area.Rooms.Where(r => !priorRoomIds.Contains(r.Id)))
            {
                dbContext.Entry(room).State = EntityState.Added;
            }
        }
    }
}
