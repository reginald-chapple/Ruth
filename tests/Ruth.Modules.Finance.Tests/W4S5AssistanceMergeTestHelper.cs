using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

internal static class W4S5AssistanceMergeTestHelper
{
    public static async Task VerifyOutboxAndPersonMergeAsync(
        IQueryDispatcher queries,
        FinanceDbContext dbContext,
        IServiceProvider sp,
        Guid caseId,
        Guid applicantPersonId
    )
    {
        var outboxTypes = await dbContext
            .OutboxMessages.AsNoTracking()
            .Select(static m => m.EventType)
            .ToListAsync();
        Assert.Contains(outboxTypes, static t =>
            t.Contains("AssistanceDisbursementRecordedIntegrationEvent", StringComparison.Ordinal)
        );

        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        var survivingPersonId = Guid.CreateVersion7();
        await consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                SurvivingPersonId: survivingPersonId,
                RetiredPersonId: applicantPersonId,
                ExternalRecordGuids: [],
                HadMultipleDistinctEmails: false
            )
        );

        var mergedCase = (
            await queries.QueryAsync<GetAssistanceCaseByIdQuery, AssistanceCaseResponse>(
                new GetAssistanceCaseByIdQuery(caseId)
            )
        ).Value;
        Assert.Equal(survivingPersonId, mergedCase.ApplicantPersonId);
    }
}
