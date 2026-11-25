using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class UpdateCheckInConfigurationCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<UpdateCheckInConfigurationCommand, CheckInConfigurationDetailResponse>
{
    public async Task<Result<CheckInConfigurationDetailResponse>> HandleAsync(
        UpdateCheckInConfigurationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = CheckInConfigurationId.From(command.ConfigurationId);
        var config = await dbContext
            .CheckInConfigurations.Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (config is null)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(
                CheckInErrors.ConfigurationNotFound
            );
        }

        var normalized = command.Name?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(normalized))
        {
            var duplicate = await dbContext.CheckInConfigurations.AnyAsync(
                c => c.Id != id && c.NormalizedName == normalized,
                cancellationToken
            );
            if (duplicate)
            {
                return Result.Failure<CheckInConfigurationDetailResponse>(
                    CheckInErrors.ConfigurationDuplicateName
                );
            }
        }

        var updated = config.Update(
            new CheckInConfigurationInput(
                command.Name ?? string.Empty,
                command.Description,
                command.ProfileKind,
                command.BranchId,
                command.SearchMode,
                command.CheckInStartOffsetMinutes,
                command.CheckInEndOffsetMinutes,
                command.SecurityCodeAlphaLength,
                command.SecurityCodeNumericLength,
                command.PreventDuplicateCheckIn,
                command.AllowCheckout,
                command.AutoSelectDaysBack,
                command.IsActive
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInConfigurationMapper.ToDetail(config));
    }
}
