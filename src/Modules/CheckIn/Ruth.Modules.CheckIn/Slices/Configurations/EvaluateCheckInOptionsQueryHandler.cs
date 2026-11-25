using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Configurations;

internal sealed class EvaluateCheckInOptionsQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<EvaluateCheckInOptionsQuery, CheckInEvaluationResponse>
{
    public async Task<Result<CheckInEvaluationResponse>> HandleAsync(
        EvaluateCheckInOptionsQuery query,
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
        if (config is null)
        {
            return Result.Failure<CheckInEvaluationResponse>(CheckInErrors.ConfigurationNotFound);
        }

        var participant = query.Participant ?? new CheckInParticipantContextDto();
        var options = config
            .Areas.OrderBy(static a => a.Order)
            .ThenBy(static a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(area =>
                EvaluateSingleArea(
                    area,
                    query.ScheduleId,
                    participant,
                    query.CurrentOccupancyByLocationId
                )
            )
            .ToList();

        return Result.Success(
            new CheckInEvaluationResponse(config.Id.Value, config.Name, query.ScheduleId, options)
        );
    }

    private static EvaluatedCheckInAreaOptionResponse EvaluateSingleArea(
        CheckInArea area,
        Guid scheduleId,
        CheckInParticipantContextDto participant,
        IReadOnlyDictionary<Guid, int>? occupancy
    )
    {
        var (isEligible, reasons) = CheckInEligibilityEvaluator.Evaluate(area, participant);
        var (recommendedId, recommendedName, rooms) = RoomBalancingDomainService.EvaluateAreaRooms(
            area,
            scheduleId,
            occupancy
        );

        return new EvaluatedCheckInAreaOptionResponse(
            area.Id.Value,
            area.Name,
            area.LinkedGroupId,
            isEligible,
            reasons,
            area.PrintBadge,
            area.IssueSecurityPickupCode,
            area.Eligibility.SupportsAccommodations,
            isEligible ? recommendedId : null,
            isEligible ? recommendedName : null,
            rooms
        );
    }
}
