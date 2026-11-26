using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class CommunicationPersonMergedConsumer(CommunicationDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;
        if (retiredId == Guid.Empty || survivingId == Guid.Empty || retiredId == survivingId)
        {
            return;
        }

        var changedSystem = await RepointSystemEntitiesAsync(
            retiredId,
            survivingId,
            cancellationToken
        );
        var changedBulk = await RepointBulkAndSmsEntitiesAsync(
            retiredId,
            survivingId,
            cancellationToken
        );
        var changedFlows = await RepointFlowEntitiesAsync(
            retiredId,
            survivingId,
            cancellationToken
        );

        if (changedSystem || changedBulk || changedFlows)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RepointSystemEntitiesAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var devices = await dbContext
            .PersonalDevices.Where(d => d.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var device in devices)
        {
            device.RepointPerson(retiredId, survivingId);
        }

        var phones = await dbContext
            .SystemPhoneNumbers.Where(p => p.AssignedToPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var phone in phones)
        {
            phone.RepointAssignedPerson(retiredId, survivingId);
        }

        var logs = await dbContext
            .SystemNotificationLogs.Where(l => l.RecipientPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var log in logs)
        {
            log.RepointPerson(retiredId, survivingId);
        }

        return devices.Count > 0 || phones.Count > 0 || logs.Count > 0;
    }

    private async Task<bool> RepointBulkAndSmsEntitiesAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var comms = await dbContext
            .Communications.Where(c =>
                c.CreatedByPersonId == retiredId || c.ReviewerPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var comm in comms)
        {
            comm.RepointPerson(retiredId, survivingId);
        }

        var recipients = await dbContext
            .CommunicationRecipients.Where(r => r.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var recipient in recipients)
        {
            recipient.RepointPerson(retiredId, survivingId);
        }

        var prefs = await dbContext
            .CommunicationSubscriptionPreferences.Where(p => p.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var pref in prefs)
        {
            pref.RepointPerson(retiredId, survivingId);
        }

        var responses = await dbContext
            .CommunicationResponses.Where(r =>
                r.ParticipantPersonId == retiredId || r.RepliedByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var response in responses)
        {
            response.RepointPerson(retiredId, survivingId);
        }

        return comms.Count > 0 || recipients.Count > 0 || prefs.Count > 0 || responses.Count > 0;
    }

    private async Task<bool> RepointFlowEntitiesAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var flowRecipients = await dbContext
            .CommunicationFlowInstanceRecipients.Where(r => r.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var item in flowRecipients)
        {
            item.RepointPerson(retiredId, survivingId);
        }

        var conversions = await dbContext
            .CommunicationFlowConversions.Where(c => c.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var conv in conversions)
        {
            conv.RepointPerson(retiredId, survivingId);
        }

        return flowRecipients.Count > 0 || conversions.Count > 0;
    }
}
