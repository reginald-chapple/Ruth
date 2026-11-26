using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

internal static class W3S6WebhooksAndTemplatesTestHelper
{
    public static async Task VerifyTemplatesAndHtmlExportAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var seededTemplates = (
            await queries.QueryAsync<
                GetCommunicationTemplatesQuery,
                IReadOnlyList<CommunicationTemplateResponse>
            >(new GetCommunicationTemplatesQuery(ActiveOnly: true))
        ).Value;
        Assert.Contains(seededTemplates, static t =>
            string.Equals(t.Key, "community-member-digest", StringComparison.Ordinal)
        );

        var preview = (
            await commands.SendAsync<PreviewEmailComposerCommand, EmailComposerPreviewResponse>(
                new PreviewEmailComposerCommand(
                    "{{ OrganizationName }} — Member Digest",
                    [
                        new EmailSectionDto(
                            0,
                            EmailSectionKind.HeroBanner,
                            Heading: "Welcome {{ RecipientName }}",
                            BodyMarkup: "Community highlights for this week."
                        ),
                        new EmailSectionDto(
                            1,
                            EmailSectionKind.ImageBlock,
                            ImageUrl: "https://example.org/community.png",
                            ImageAltText: "Community center gathering"
                        ),
                        new EmailSectionDto(
                            2,
                            EmailSectionKind.CallToActionButton,
                            ButtonLabel: "Explore Events",
                            ButtonUrl: "https://example.org/events"
                        ),
                    ]
                )
            )
        ).Value;

        var exportDir = Path.Combine(FindRepoRoot(), "artifacts", "htmlhint");
        _ = Directory.CreateDirectory(exportDir);
        await File.WriteAllTextAsync(
            Path.Combine(exportDir, "w3s6-structured-digest.html"),
            preview.RenderedHtml
        );
    }

    public static async Task VerifyWebhooksAsync(
        ICommandDispatcher commands,
        Guid deliveredRecipientId
    )
    {
        var webhookRes = (
            await commands.SendAsync<
                ProcessCommunicationWebhooksCommand,
                CommunicationWebhookProcessResponse
            >(
                new ProcessCommunicationWebhooksCommand([
                    new CommunicationWebhookEventDto(
                        CommunicationWebhookEventType.Opened,
                        RecipientId: deliveredRecipientId
                    ),
                    new CommunicationWebhookEventDto(
                        CommunicationWebhookEventType.Clicked,
                        RecipientId: deliveredRecipientId,
                        ClickedUrl: "https://example.org/events"
                    ),
                    new CommunicationWebhookEventDto(
                        CommunicationWebhookEventType.Bounced,
                        RecipientId: deliveredRecipientId,
                        BounceKind: BounceKind.Hard,
                        Reason: "550 Mailbox unavailable"
                    ),
                ])
            )
        ).Value;
        Assert.Equal(3, webhookRes.ProcessedCount);
        Assert.Equal(1, webhookRes.SuppressionsRecorded);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ruth.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
