using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Infrastructure.Templating.BuiltInTemplates;

namespace Ruth.Modules.Communication.Infrastructure.Templating;

internal static class BuiltInRazorTemplatesCatalog
{
    public const string StandardEmailKey = "Communication.StandardEmail";
    public const string SimpleAlertKey = "Communication.SimpleAlert";

    public static RazorTemplateDescriptor[] GetAll()
    {
        return [CreateStandardEmailDescriptor(), CreateSimpleAlertDescriptor()];
    }

    private static RazorTemplateDescriptor CreateStandardEmailDescriptor()
    {
        return new RazorTemplateDescriptor(
            StandardEmailKey,
            "Standard Email Layout",
            "Responsive transactional and announcement email shell with header, mergeable HTML/text body, optional CTA button, and footer.",
            "Email",
            typeof(StandardEmailTemplate),
            [
                new(
                    "OrganizationName",
                    "string",
                    IsRequired: false,
                    "Header organization label (defaults to Ruth)."
                ),
                new("Subject", "string", IsRequired: false, "HTML title and subject line."),
                new("Preheader", "string", IsRequired: false, "Hidden inbox preview text."),
                new("Heading", "string", IsRequired: false, "Primary banner heading."),
                new(
                    "BodyText",
                    "string",
                    IsRequired: false,
                    "Plain-text message body (HTML-encoded)."
                ),
                new(
                    "BodyHtml",
                    "string",
                    IsRequired: false,
                    "Pre-rendered HTML body (supports merge tokens)."
                ),
                new("ActionLabel", "string", IsRequired: false, "Optional CTA button caption."),
                new(
                    "ActionUrl",
                    "string",
                    IsRequired: false,
                    "Optional CTA button URL (http/https/mailto/relative)."
                ),
                new("FooterText", "string", IsRequired: false, "Optional footer text."),
            ]
        );
    }

    private static RazorTemplateDescriptor CreateSimpleAlertDescriptor()
    {
        return new RazorTemplateDescriptor(
            SimpleAlertKey,
            "Simple Notification Card",
            "Compact tone-styled notification card (info, success, warning, error) for alerts and system messages.",
            "Notifications",
            typeof(SimpleAlertTemplate),
            [
                new("Title", "string", IsRequired: false, "Notification heading."),
                new("Message", "string", IsRequired: false, "Plain-text notification body."),
                new(
                    "MessageHtml",
                    "string",
                    IsRequired: false,
                    "Optional pre-rendered safe HTML body."
                ),
                new("Footnote", "string", IsRequired: false, "Optional muted footnote."),
                new(
                    "Tone",
                    "string",
                    IsRequired: false,
                    "Visual accent tone: info, success, warning, or error."
                ),
            ]
        );
    }
}
