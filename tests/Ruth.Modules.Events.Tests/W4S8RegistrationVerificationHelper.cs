using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Events.Contracts.Events;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.Modules.Events.Slices.Registrations;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Events.Tests;

internal static class W4S8RegistrationVerificationHelper
{
    public static async Task VerifyCancelWaitlistMergeAndJobAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        RegistrationResponse reg1,
        RegistrationResponse reg2,
        Guid person1Id,
        Guid person2Id
    )
    {
        var firstRegistrantId = reg1.Registrants[0].Id;
        var cancelRes = await commands.SendAsync<
            CancelRegistrantCommand,
            CancelRegistrantResultResponse
        >(new CancelRegistrantCommand(reg1.Id, firstRegistrantId));
        Assert.True(cancelRes.IsSuccess);
        var promotedId = Assert.Single(cancelRes.Value.AutoPromotedRegistrantIds);
        Assert.Equal(reg2.Registrants[0].Id, promotedId);

        var refreshedReg2 = await queries.QueryAsync<
            GetRegistrationByConfirmationOrIdQuery,
            RegistrationResponse
        >(new GetRegistrationByConfirmationOrIdQuery(reg2.ConfirmationCode));
        Assert.True(refreshedReg2.IsSuccess);
        var promotedRegistrant = Assert.Single(refreshedReg2.Value.Registrants);
        Assert.Equal(RegistrantStatus.Confirmed, promotedRegistrant.Status);

        var db = sp.GetRequiredService<EventsDbContext>();
        var outboxTypes = await db.OutboxMessages.Select(static m => m.EventType).ToListAsync();
        var hasOutbox = outboxTypes.Exists(static t =>
            t.Contains(
                nameof(RegistrantPromotedFromWaitlistIntegrationEvent),
                StringComparison.Ordinal
            )
        );
        Assert.True(hasOutbox);

        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                person1Id,
                person2Id,
                [],
                HadMultipleDistinctEmails: false
            )
        );

        var mergedReg2 = await queries.QueryAsync<
            GetRegistrationByConfirmationOrIdQuery,
            RegistrationResponse
        >(new GetRegistrationByConfirmationOrIdQuery(reg2.ConfirmationCode));
        Assert.True(mergedReg2.IsSuccess);
        Assert.Equal(person1Id, mergedReg2.Value.RegistrarPersonId);

        var jobs = sp.GetServices<IScheduledJob>();
        var cleanupJob = Assert.Single(jobs, static j => j is ExpiredRegistrationSessionCleanupJob);
        var jobResult = await cleanupJob.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobResult.Status);
    }
}
