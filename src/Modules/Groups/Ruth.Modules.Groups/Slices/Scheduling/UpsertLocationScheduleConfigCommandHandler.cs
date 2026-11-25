using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class UpsertLocationScheduleConfigCommandHandler(GroupsDbContext dbContext)
    : ICommandHandler<UpsertLocationScheduleConfigCommand, GroupLocationScheduleConfigResponse>
{
    public async Task<Result<GroupLocationScheduleConfigResponse>> HandleAsync(
        UpsertLocationScheduleConfigCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var glId = GroupLocationId.From(command.GroupLocationId);
        var groupLocation = await dbContext
            .GroupLocations.AsNoTracking()
            .FirstOrDefaultAsync(gl => gl.Id == glId, cancellationToken);
        if (groupLocation is null)
        {
            return GroupsErrors.GroupLocationNotFound;
        }

        var existing = await dbContext.LocationScheduleConfigs.FirstOrDefaultAsync(
            c => c.GroupLocationId == glId && c.ScheduleId == command.ScheduleId,
            cancellationToken
        );

        GroupLocationScheduleConfig entity;
        if (existing is not null)
        {
            var updated = existing.Update(
                command.ConfigurationName,
                command.MinimumCapacity,
                command.DesiredCapacity,
                command.MaximumCapacity,
                command.ConfirmationAdditionalDetails,
                command.ReminderAdditionalDetails
            );
            if (updated.IsFailure)
            {
                return Result.Failure<GroupLocationScheduleConfigResponse>(updated.Errors);
            }

            entity = existing;
        }
        else
        {
            var created = GroupLocationScheduleConfig.Create(
                glId,
                command.ScheduleId,
                command.ConfigurationName,
                command.MinimumCapacity,
                command.DesiredCapacity,
                command.MaximumCapacity,
                command.ConfirmationAdditionalDetails,
                command.ReminderAdditionalDetails
            );
            if (created.IsFailure)
            {
                return Result.Failure<GroupLocationScheduleConfigResponse>(created.Errors);
            }

            entity = created.Value;
            _ = dbContext.LocationScheduleConfigs.Add(entity);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(entity, groupLocation));
    }

    internal static GroupLocationScheduleConfigResponse ToResponse(
        GroupLocationScheduleConfig config,
        GroupLocation location
    )
    {
        return new GroupLocationScheduleConfigResponse(
            config.Id.Value,
            config.GroupLocationId.Value,
            location.GroupId.Value,
            location.LocationId,
            location.LocationName,
            config.ScheduleId,
            config.ConfigurationName,
            config.MinimumCapacity,
            config.DesiredCapacity,
            config.MaximumCapacity,
            config.ConfirmationAdditionalDetails,
            config.ReminderAdditionalDetails
        );
    }
}
