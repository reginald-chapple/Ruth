using System.Globalization;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Slices.Flows;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class GroupAttendanceNotificationConsumer(
    ICommandDispatcher commandDispatcher,
    FlowAttendanceConversionEvaluator? flowConversionEvaluator = null
) : IIntegrationEventConsumer<PersonAttendedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonAttendedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OccurrenceDate"] = integrationEvent.OccurrenceDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            ["IsFirstTime"] = integrationEvent.IsFirstTime ? "true" : "false",
        };

        _ = await commandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                "groups.attendance-recorded",
                RecipientPersonId: integrationEvent.PersonId,
                MergeFields: merge,
                TriggerSource: "Groups.PersonAttended"
            ),
            cancellationToken
        );

        if (flowConversionEvaluator is not null)
        {
            await flowConversionEvaluator.EvaluateAttendanceConversionAsync(
                integrationEvent.PersonId,
                integrationEvent.GroupId,
                cancellationToken
            );
        }
    }
}
