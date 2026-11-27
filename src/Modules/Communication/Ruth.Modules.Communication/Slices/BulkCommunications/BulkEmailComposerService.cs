using System.Text;
using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class BulkEmailComposerService(
    IMergeTemplateEngine mergeEngine,
    IRazorTemplateRenderer razorRenderer,
    IOptions<CommunicationTransportOptions> transportOptions,
    IOptions<CommunicationBulkOptions> bulkOptions
)
{
    private readonly CommunicationTransportOptions _transportOptions = transportOptions.Value;
    private readonly CommunicationBulkOptions _bulkOptions = bulkOptions.Value;

    public string MintPreferenceToken(
        CommunicationChannel channel,
        string normalizedDestination,
        Guid? personId
    )
    {
        return PreferenceTokenSigner.Sign(
            channel,
            normalizedDestination,
            personId,
            _bulkOptions.PreferenceSigningSecret,
            AmbientTenantContext.CurrentTenantId
        );
    }

    public async Task<Result<EmailComposerPreviewResponse>> RenderEmailAsync(
        string subjectTemplate,
        string? preheaderText,
        string? razorLayoutKey,
        IReadOnlyList<EmailSection> sections,
        string? htmlBodyOverride,
        bool includeUnsubscribeFooter,
        string topicKey,
        string? recipientName,
        string? normalizedDestination,
        Guid? personId,
        IReadOnlyDictionary<string, string?>? mergeFields,
        CancellationToken cancellationToken
    )
    {
        var context = BuildMergeContext(
            topicKey,
            recipientName,
            normalizedDestination,
            personId,
            mergeFields
        );

        var subjectResult = await mergeEngine.RenderAsync(
            subjectTemplate,
            context,
            MergeRenderOptions.DefaultPlainText,
            cancellationToken
        );
        if (subjectResult.IsFailure)
        {
            return Result.Failure<EmailComposerPreviewResponse>(subjectResult.Errors);
        }

        var rawBodyMarkup =
            sections.Count > 0
                ? EmailSectionHtmlComposer.ComposeBodyMarkup(sections, includeUnsubscribeFooter)
                : htmlBodyOverride ?? string.Empty;

        var bodyFragmentResult = await mergeEngine.RenderAsync(
            rawBodyMarkup,
            context,
            MergeRenderOptions.DefaultHtml,
            cancellationToken
        );
        if (bodyFragmentResult.IsFailure)
        {
            return Result.Failure<EmailComposerPreviewResponse>(bodyFragmentResult.Errors);
        }

        var fullHtmlResult = await WrapInLayoutAsync(
            razorLayoutKey,
            subjectResult.Value.RenderedContent,
            preheaderText,
            bodyFragmentResult.Value.RenderedContent,
            cancellationToken
        );
        return fullHtmlResult.IsFailure
            ? Result.Failure<EmailComposerPreviewResponse>(fullHtmlResult.Errors)
            : new EmailComposerPreviewResponse(
                subjectResult.Value.RenderedContent,
                fullHtmlResult.Value,
                StripHtml(bodyFragmentResult.Value.RenderedContent)
            );
    }

    public async Task<Result<string>> RenderPlainTemplateAsync(
        string template,
        string topicKey,
        string? recipientName,
        string? normalizedDestination,
        Guid? personId,
        IReadOnlyDictionary<string, string?>? mergeFields,
        CancellationToken cancellationToken
    )
    {
        var context = BuildMergeContext(
            topicKey,
            recipientName,
            normalizedDestination,
            personId,
            mergeFields,
            CommunicationChannel.Sms
        );
        var rendered = await mergeEngine.RenderAsync(
            template,
            context,
            MergeRenderOptions.DefaultPlainText,
            cancellationToken
        );
        return rendered.IsFailure
            ? Result.Failure<string>(rendered.Errors)
            : rendered.Value.RenderedContent;
    }

    private Dictionary<string, object?> BuildMergeContext(
        string topicKey,
        string? recipientName,
        string? normalizedDestination,
        Guid? personId,
        IReadOnlyDictionary<string, string?>? mergeFields,
        CommunicationChannel channel = CommunicationChannel.Email
    )
    {
        var baseUrl = _bulkOptions.PublicBaseUrl.TrimEnd('/');
        var dest = string.IsNullOrWhiteSpace(normalizedDestination)
            ? "preview@example.org"
            : normalizedDestination;
        var token = MintPreferenceToken(channel, dest, personId);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedTopic = Uri.EscapeDataString(topicKey);

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrganizationName"] = _transportOptions.DefaultOrganizationName,
            ["RecipientName"] = string.IsNullOrWhiteSpace(recipientName)
                ? null
                : recipientName.Trim(),
            ["Email"] = dest,
            ["TopicKey"] = topicKey,
            ["PreferenceCenterUrl"] = $"{baseUrl}/communication/preferences?token={encodedToken}",
            ["UnsubscribeUrl"] =
                $"{baseUrl}/api/communication/preferences/unsubscribe-link?token={encodedToken}&topic={encodedTopic}",
        };

        if (mergeFields is not null)
        {
            foreach (var kvp in mergeFields)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Key))
                {
                    dict[kvp.Key.Trim()] = kvp.Value;
                }
            }
        }

        return dict;
    }

    private async Task<Result<string>> WrapInLayoutAsync(
        string? razorLayoutKey,
        string subject,
        string? preheader,
        string bodyHtml,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(razorLayoutKey))
        {
            return $"<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" /><title>{System.Net.WebUtility.HtmlEncode(subject)}</title></head><body>{bodyHtml}</body></html>";
        }

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["OrganizationName"] = _transportOptions.DefaultOrganizationName,
            ["Subject"] = subject,
            ["Preheader"] = preheader,
            ["Heading"] = subject,
            ["BodyHtml"] = bodyHtml,
        };
        return await razorRenderer.RenderNamedTemplateAsync(
            razorLayoutKey,
            parameters,
            cancellationToken
        );
    }

    private static string StripHtml(string html)
    {
        var sb = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var ch in html)
        {
            if (ch == '<')
            {
                inTag = true;
            }
            else if (ch == '>')
            {
                inTag = false;
                _ = sb.Append(' ');
            }
            else if (!inTag)
            {
                _ = sb.Append(ch);
            }
        }

        return string.Join(
            ' ',
            sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
        );
    }
}
