using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Loads the Volunteer Schedule Toolbox for a Person.</summary>
public sealed record GetVolunteerScheduleToolboxQuery(Guid PersonId, DateOnly? FromDate = null)
    : IQuery<VolunteerScheduleToolboxResponse>;
