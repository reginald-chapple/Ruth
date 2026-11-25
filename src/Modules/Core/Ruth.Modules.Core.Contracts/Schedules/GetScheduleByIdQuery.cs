using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Schedules;

/// <summary>Gets a single schedule by identifier.</summary>
public sealed record GetScheduleByIdQuery(Guid ScheduleId) : IQuery<ScheduleResponse>;
