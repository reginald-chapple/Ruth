using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure.Templating;

namespace Ruth.Modules.Communication.Infrastructure;

internal static class BuiltInSystemCommunicationsSeeder
{
    public static async Task SeedAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        await SeedSnippetsAsync(dbContext, utcNow, cancellationToken);
        await SeedSystemCommunicationsAsync(dbContext, utcNow, cancellationToken);
        await BuiltInBulkAndSmsSeeder.SeedAsync(dbContext, utcNow, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSnippetsAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .TemplateSnippets.Select(static s => s.Key)
            .ToListAsync(cancellationToken);
        var keySet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        AddSnippetIfMissing(
            dbContext,
            keySet,
            utcNow,
            "Organization Footer",
            "org_footer",
            "<p style=\"margin:16px 0 0;font-size:12px;color:#6b7280;\">Sent by {{ OrganizationName | default:'Community Organization' }}.</p>",
            CommunicationSystemGuids.OrgFooterSnippet
        );
        AddSnippetIfMissing(
            dbContext,
            keySet,
            utcNow,
            "Support Contact Notice",
            "support_contact",
            "<span>Need assistance? Contact {{ SupportEmail | default:'support@example.org' }}.</span>",
            CommunicationSystemGuids.SupportContactSnippet
        );
    }

    private static void AddSnippetIfMissing(
        CommunicationDbContext dbContext,
        HashSet<string> keySet,
        DateTimeOffset utcNow,
        string name,
        string key,
        string markup,
        Guid systemGuid
    )
    {
        if (keySet.Contains(key))
        {
            return;
        }

        var created = TemplateSnippet.Create(
            name,
            key,
            markup,
            description: $"Built-in {name} snippet",
            outputMode: TemplateOutputMode.Html,
            isSystem: true,
            isActive: true,
            systemGuid: systemGuid
        );
        if (created.IsSuccess)
        {
            created.Value.SetCreated(utcNow);
            _ = dbContext.TemplateSnippets.Add(created.Value);
        }
    }

    private static async Task SeedSystemCommunicationsAsync(
        CommunicationDbContext dbContext,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .SystemCommunications.Select(static c => c.Key)
            .ToListAsync(cancellationToken);
        var keySet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in BuildDefaultSpecs())
        {
            if (keySet.Contains(spec.Key))
            {
                continue;
            }

            var created = SystemCommunication.Create(spec);
            if (created.IsSuccess)
            {
                created.Value.SetCreated(utcNow);
                _ = dbContext.SystemCommunications.Add(created.Value);
            }
        }
    }

    private static SystemCommunicationSpec[] BuildDefaultSpecs()
    {
        return [.. BuildIdentityAndGroupSpecs(), .. BuildFinanceAndWorkflowSpecs()];
    }

    private static SystemCommunicationSpec[] BuildIdentityAndGroupSpecs()
    {
        var layout = BuiltInRazorTemplatesCatalog.StandardEmailKey;
        return
        [
            new(
                "Organization Onboarding Completed",
                "identity.organization-onboarded",
                SystemCommunicationCategory.IdentityAndSecurity,
                "Welcome to {{ OrganizationName | default:'Your Organization' }}",
                "<p>Your organization <strong>{{ OrganizationName }}</strong> is ready in timezone {{ TimeZoneId }}.</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "{{ OrganizationName }} onboarding completed.",
                PushTitleTemplate: "Workspace Ready",
                PushMessageTemplate: "{{ OrganizationName }} is now live.",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommOrganizationOnboarded
            ),
            new(
                "Password Reset Request",
                "identity.password-reset",
                SystemCommunicationCategory.IdentityAndSecurity,
                "Reset your {{ OrganizationName | default:'Your Organization' }} password",
                "<p>Hello {{ RecipientName | default:'Member' }},</p><p>Use reset code <strong>{{ ResetToken }}</strong> before {{ ExpiresOnUtc }}.</p><p>{{ snippet:support_contact }}</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "Security reset code for {{ UserName }}: {{ ResetToken }}",
                PushTitleTemplate: "Password Reset Requested",
                PushMessageTemplate: "A password reset was requested for {{ UserName }}.",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommPasswordReset
            ),
            new(
                "Account Email Confirmation",
                "identity.account-confirmation",
                SystemCommunicationCategory.IdentityAndSecurity,
                "Confirm your {{ OrganizationName | default:'Your Organization' }} account",
                "<p>Hello {{ RecipientName | default:'Member' }},</p><p>Confirm your account with token <strong>{{ ConfirmationToken }}</strong> before {{ ExpiresOnUtc }}.</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "Confirm account {{ UserName }} with code {{ ConfirmationToken }}",
                PushTitleTemplate: "Confirm Your Account",
                PushMessageTemplate: "Complete email verification for {{ UserName }}.",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommAccountConfirmation
            ),
            new(
                "Group & Program Attendance Confirmation",
                "groups.attendance-recorded",
                SystemCommunicationCategory.GroupsAndScheduling,
                "Attendance confirmed for {{ OccurrenceDate }}",
                "<p>Hello {{ RecipientName | default:'Participant' }},</p><p>Your attendance on <strong>{{ OccurrenceDate }}</strong> has been recorded.</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "Attendance confirmed for {{ OccurrenceDate }}.",
                PushTitleTemplate: "Attendance Recorded",
                PushMessageTemplate: "Checked in on {{ OccurrenceDate }}.",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommGroupAttendanceRecorded
            ),
        ];
    }

    private static SystemCommunicationSpec[] BuildFinanceAndWorkflowSpecs()
    {
        var layout = BuiltInRazorTemplatesCatalog.StandardEmailKey;
        return
        [
            new(
                "Payment, Dues, or Gift Receipt",
                "finance.payment-or-gift-receipt",
                SystemCommunicationCategory.FinanceAndBilling,
                "Receipt {{ ReceiptNumber | default:'Confirmation' }} — {{ OrganizationName | default:'Your Organization' }}",
                "<p>Hello {{ RecipientName | default:'Supporter' }},</p><p>We received your transaction of <strong>{{ Amount }}</strong> toward <strong>{{ AccountName | default:'General Fund' }}</strong> ({{ TaxDeductibleNotice | default:'Standard receipt' }}).</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "Receipt {{ ReceiptNumber }}: {{ Amount }} received for {{ AccountName | default:'General' }}.",
                PushTitleTemplate: "Receipt Available",
                PushMessageTemplate: "Your {{ Amount }} transaction receipt is ready.",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommFinanceReceipt
            ),
            new(
                "Workflow Task Assignment",
                "workflow.task-assigned",
                SystemCommunicationCategory.WorkflowAndOperations,
                "Action Required: {{ TaskTitle | default:'Assigned Task' }}",
                "<p>Hello {{ RecipientName | default:'Team Member' }},</p><p>You have been assigned workflow task <strong>{{ TaskTitle }}</strong> in <strong>{{ WorkflowName | default:'Operations' }}</strong>.</p>{{ snippet:org_footer }}",
                RazorLayoutKey: layout,
                SmsMessageTemplate: "Assigned task: {{ TaskTitle }} ({{ WorkflowName | default:'Operations' }}).",
                PushTitleTemplate: "New Task Assigned",
                PushMessageTemplate: "{{ TaskTitle }} requires your review.",
                PushOpenUrlTemplate: "{{ ActionUrl }}",
                IsSystem: true,
                SystemGuid: CommunicationSystemGuids.SysCommWorkflowTaskAssigned
            ),
        ];
    }
}
