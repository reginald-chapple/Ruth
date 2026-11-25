using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Loads the Coordinator Schedule Grid for a Group across a date window.</summary>
public sealed record GetServingScheduleGridQuery(Guid GroupId, DateOnly StartDate, DateOnly EndDate)
    : IQuery<ServingScheduleGridResponse>;
