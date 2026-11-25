using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class ScheduleVolunteerCommandHandler(ScheduleVolunteerService service)
    : ICommandHandler<ScheduleVolunteerCommand, ScheduledVolunteerResponse>
{
    public Task<Result<ScheduledVolunteerResponse>> HandleAsync(
        ScheduleVolunteerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        return service.ScheduleAsync(command, cancellationToken);
    }
}
