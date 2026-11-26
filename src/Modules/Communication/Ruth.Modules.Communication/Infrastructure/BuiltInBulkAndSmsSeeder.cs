using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.Modules.Communication.Infrastructure.Templating;

namespace Ruth.Modules.Communication.Infrastructure;

internal static class BuiltInBulkAndSmsSeeder
{
    public static async Task SeedAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        await SeedDefaultCommunicationTemplateAsync(dbContext, utcNow, cancellationToken);
        await SeedDefaultSmsPipelineAsync(dbContext, utcNow, cancellationToken);
        await SeedDefaultOnboardingFlowAsync(dbContext, utcNow, cancellationToken);
    }

    private static async Task SeedDefaultCommunicationTemplateAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var exists = await dbContext.CommunicationTemplates.AnyAsync(
            static t => t.Key == "community-member-digest",
            cancellationToken
        );
        if (exists)
        {
            return;
        }

        var created = CommunicationTemplate.Create(
            new CommunicationTemplateSpec(
                "Member & Community Digest",
                "community-member-digest",
                CommunicationChannel.Email,
                TopicKey: "community-digest",
                SubjectTemplate: "{{ OrganizationName | default:'Ruth' }} — Weekly Community Digest",
                Description: "Structured multi-section announcement and upcoming program digest.",
                PreheaderText: "Highlights, programs, and member updates for this week.",
                RazorLayoutKey: BuiltInRazorTemplatesCatalog.StandardEmailKey,
                Sections:
                [
                    new EmailSectionDto(
                        0,
                        EmailSectionKind.HeroBanner,
                        Heading: "Hello {{ RecipientName | default:'Member' }}",
                        BodyMarkup: "Here is what is happening across {{ OrganizationName | default:'our community' }} this week."
                    ),
                    new EmailSectionDto(
                        1,
                        EmailSectionKind.RichText,
                        Heading: "Featured Update",
                        BodyMarkup: "<p>{{ SummaryText | default:'Explore upcoming programs, volunteer opportunities, and member resources.' }}</p>"
                    ),
                    new EmailSectionDto(
                        2,
                        EmailSectionKind.CallToActionButton,
                        ButtonLabel: "View Member Portal",
                        ButtonUrl: "https://example.org/portal"
                    ),
                ],
                IsSystem: true,
                IsActive: true,
                SystemGuid: CommunicationSystemGuids.SeededMemberDigestTemplate
            )
        );
        if (created.IsSuccess)
        {
            created.Value.SetCreated(utcNow);
            _ = dbContext.CommunicationTemplates.Add(created.Value);
        }
    }

    private static async Task SeedDefaultSmsPipelineAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var exists = await dbContext.SmsPipelines.AnyAsync(
            p => p.SystemGuid == CommunicationSystemGuids.SeededMemberSmsPipeline,
            cancellationToken
        );
        if (exists)
        {
            return;
        }

        var created = SmsPipeline.Create(
            "Standard Member SMS Responder",
            [
                new SmsActionInputDto(
                    "Help Keyword Reply",
                    0,
                    SmsKeywordMatchMode.ExactKeyword,
                    "HELP",
                    SmsActionKind.AutoReply,
                    ReplyTemplate: "{{ OrganizationName | default:'Ruth' }} Support: Reply STOP to opt out or START to resume messages."
                ),
                new SmsActionInputDto(
                    "Hours & Location Reply",
                    1,
                    SmsKeywordMatchMode.ExactKeyword,
                    "HOURS",
                    SmsActionKind.AutoReply,
                    ReplyTemplate: "{{ OrganizationName | default:'Ruth' }} operating hours are Monday–Friday 8:00 AM – 6:00 PM."
                ),
            ],
            description: "Built-in compliance and member support keyword responder.",
            priorityOrder: 100,
            isActive: true,
            isSystem: true,
            systemGuid: CommunicationSystemGuids.SeededMemberSmsPipeline
        );
        if (created.IsSuccess)
        {
            created.Value.SetCreated(utcNow);
            _ = dbContext.SmsPipelines.Add(created.Value);
        }
    }

    private static async Task SeedDefaultOnboardingFlowAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var exists = await dbContext.CommunicationFlows.AnyAsync(
            static f => f.Key == "new-member-onboarding-drip",
            cancellationToken
        );
        if (exists)
        {
            return;
        }

        var created = CommunicationFlow.Create(
            new CommunicationFlowSpec(
                "New Member Onboarding Drip",
                "new-member-onboarding-drip",
                CommunicationFlowCategory.Onboarding,
                BuildDefaultOnboardingSteps(),
                TopicKey: "general-announcements",
                Description: "Timed 3-step welcome and orientation drip campaign for new members.",
                TriggerMode: FlowTriggerMode.RollingEnrollment,
                ExitCondition: FlowExitCondition.ExitOnConversion,
                ConversionGoalKind: FlowConversionGoalKind.AttendedSession,
                ConversionWindowDays: 30,
                IsSystem: true,
                IsActive: true,
                SystemGuid: CommunicationSystemGuids.SeededMemberOnboardingFlow
            )
        );
        if (created.IsSuccess)
        {
            created.Value.SetCreated(utcNow);
            _ = dbContext.CommunicationFlows.Add(created.Value);
        }
    }

    private static CommunicationFlowStepDto[] BuildDefaultOnboardingSteps()
    {
        return
        [
            new CommunicationFlowStepDto(
                0,
                "day-0-welcome",
                "Immediate Welcome Email",
                CommunicationChannel.Email,
                DelayMinutes: 0,
                SubjectTemplate: "Welcome to {{ OrganizationName | default:'Ruth' }}, {{ RecipientName | default:'Member' }}!",
                BodyTemplate: "<p>We are thrilled to have you join our community. Explore your member resources anytime.</p>"
            ),
            new CommunicationFlowStepDto(
                1,
                "day-3-orientation",
                "Day 3 Orientation Follow-Up",
                CommunicationChannel.Email,
                DelayMinutes: 4_320,
                SubjectTemplate: "Next Steps with {{ OrganizationName | default:'Ruth' }}",
                BodyTemplate: "<p>Ready to connect with a group, workshop, or upcoming session?</p>",
                SendOnlyIfPreviousUnopened: false
            ),
            new CommunicationFlowStepDto(
                2,
                "day-7-sms-checkin",
                "Day 7 Unopened Reminder",
                CommunicationChannel.Email,
                DelayMinutes: 5_760,
                SubjectTemplate: "Checking in from {{ OrganizationName | default:'Ruth' }}",
                BodyTemplate: "<p>Let us know if we can help you find the right cohort or program.</p>",
                SendOnlyIfPreviousUnopened: true
            ),
        ];
    }
}
