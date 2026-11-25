using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Removes/cancels a volunteer scheduling assignment.</summary>
public sealed record UnscheduleVolunteerCommand(Guid AttendanceId) : ICommand<bool>;
