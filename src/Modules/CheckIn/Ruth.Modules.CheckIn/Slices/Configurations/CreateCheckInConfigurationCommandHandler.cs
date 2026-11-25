using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class CreateCheckInConfigurationCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<CreateCheckInConfigurationCommand, CheckInConfigurationDetailResponse>
{
    public async Task<Result<CheckInConfigurationDetailResponse>> HandleAsync(
        CreateCheckInConfigurationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var normalized = command.Name?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!string.IsNullOrEmpty(normalized))
        {
            var exists = await dbContext.CheckInConfigurations.AnyAsync(
                c => c.NormalizedName == normalized,
                cancellationToken
            );
            if (exists)
            {
                return Result.Failure<CheckInConfigurationDetailResponse>(
                    CheckInErrors.ConfigurationDuplicateName
                );
            }
        }

        var created = CheckInConfiguration.Create(
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
        if (created.IsFailure)
        {
            return Result.Failure<CheckInConfigurationDetailResponse>(created.Errors);
        }

        _ = dbContext.CheckInConfigurations.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInConfigurationMapper.ToDetail(created.Value));
    }
}
