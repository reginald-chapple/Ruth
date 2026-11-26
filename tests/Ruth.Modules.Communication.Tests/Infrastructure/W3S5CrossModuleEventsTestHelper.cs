using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Groups.Contracts.Events;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

internal static class W3S5CrossModuleEventsTestHelper
{
    public static async Task ConsumeAllSampleEventsAsync(
        IServiceProvider sp,
        Guid recipientPersonId
    )
    {
        var now = DateTimeOffset.UtcNow;

        await sp.GetRequiredService<
            IIntegrationEventConsumer<OrganizationOnboardedIntegrationEvent>
        >()
            .ConsumeAsync(
                new OrganizationOnboardedIntegrationEvent(
                    Guid.CreateVersion7(),
                    "Harbor Civic Alliance",
                    "America/Denver",
                    now
                )
            );

        await sp.GetRequiredService<
            IIntegrationEventConsumer<PasswordResetRequestedIntegrationEvent>
        >()
            .ConsumeAsync(
                new PasswordResetRequestedIntegrationEvent(
                    Guid.CreateVersion7(),
                    "jordan@harbor.org",
                    "jordan",
                    "RST-889900",
                    now.AddMinutes(30),
                    now
                )
            );

        await sp.GetRequiredService<
            IIntegrationEventConsumer<AccountConfirmationRequestedIntegrationEvent>
        >()
            .ConsumeAsync(
                new AccountConfirmationRequestedIntegrationEvent(
                    Guid.CreateVersion7(),
                    "jordan@harbor.org",
                    "jordan",
                    "CNF-112233",
                    now.AddHours(24),
                    now
                )
            );

        await sp.GetRequiredService<IIntegrationEventConsumer<PersonAttendedIntegrationEvent>>()
            .ConsumeAsync(
                new PersonAttendedIntegrationEvent(
                    Guid.CreateVersion7(),
                    now,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    recipientPersonId,
                    GroupId: Guid.CreateVersion7(),
                    GroupTypeId: null,
                    BranchId: null,
                    LocationId: null,
                    ScheduleId: null,
                    new DateOnly(2026, 11, 25),
                    new DateOnly(2026, 11, 29),
                    IsFirstTime: true,
                    CountsTowardHeadlineAttendance: true
                )
            );

        var sysNotifier = sp.GetRequiredService<
            IIntegrationEventConsumer<SystemNotificationRequestedIntegrationEvent>
        >();
        await sysNotifier.ConsumeAsync(
            new SystemNotificationRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                "finance.payment-or-gift-receipt",
                "donor@harbor.org",
                "Taylor Morgan",
                "+15554443322",
                recipientPersonId,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["ReceiptNumber"] = "FIN-2026-901",
                    ["Amount"] = "$500.00",
                    ["AccountName"] = "Youth STEM Grant Fund",
                    ["TaxDeductibleNotice"] = "Tax-deductible charitable contribution",
                },
                "Finance"
            )
        );
        await sysNotifier.ConsumeAsync(
            new SystemNotificationRequestedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                "workflow.task-assigned",
                "ops@harbor.org",
                "Casey Lin",
                "+15556667788",
                recipientPersonId,
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["TaskTitle"] = "Approve Facility Rental #42",
                    ["WorkflowName"] = "Facility Operations",
                    ["ActionUrl"] = "https://harbor.org/workflows/42",
                },
                "Workflow"
            )
        );
    }

    public static Task ConsumePersonMergeAsync(
        IServiceProvider sp,
        Guid retiredPersonId,
        Guid survivingPersonId
    )
    {
        var consumer = sp.GetRequiredService<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>
        >();
        return consumer.ConsumeAsync(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                DateTimeOffset.UtcNow,
                survivingPersonId,
                retiredPersonId,
                ExternalRecordGuids: [],
                HadMultipleDistinctEmails: false
            )
        );
    }
}
