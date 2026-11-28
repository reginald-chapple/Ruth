using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure.Transports;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class SystemCommunicationRenderService(
    IMergeTemplateEngine mergeEngine,
    IRazorTemplateRenderer razorRenderer,
    IOptions<CommunicationTransportOptions> transportOptions
)
{
    private readonly CommunicationTransportOptions _options = transportOptions.Value;

    public async Task<Result<RenderedNotificationBundle>> RenderAsync(
        SystemCommunication template,
        string? recipientName,
        IReadOnlyDictionary<string, string?>? mergeFields,
        CancellationToken cancellationToken
    )
    {
        var context = BuildMergeContext(recipientName, mergeFields);

        var subjectResult = await mergeEngine.RenderAsync(
            template.SubjectTemplate,
            context,
            MergeRenderOptions.DefaultPlainText,
            cancellationToken
        );
        if (subjectResult.IsFailure)
        {
            return Result.Failure<RenderedNotificationBundle>(subjectResult.Errors);
        }

        var bodyFragmentResult = await mergeEngine.RenderAsync(
            template.HtmlBodyTemplate,
            context,
            MergeRenderOptions.DefaultHtml,
            cancellationToken
        );
        if (bodyFragmentResult.IsFailure)
        {
            return Result.Failure<RenderedNotificationBundle>(bodyFragmentResult.Errors);
        }

        var sanitizedSubject = EmailHeaderSanitizer.SanitizeHeaderValue(
            subjectResult.Value.RenderedContent
        );
        var sanitizedBodyFragment = AllowlistHtmlSanitizer.SanitizeFragment(
            bodyFragmentResult.Value.RenderedContent,
            allowMergeTokensInUrls: false
        );

        var fullHtmlResult = await WrapInRazorLayoutIfConfiguredAsync(
            template,
            sanitizedSubject,
            sanitizedBodyFragment,
            context,
            cancellationToken
        );
        return fullHtmlResult.IsFailure
            ? Result.Failure<RenderedNotificationBundle>(fullHtmlResult.Errors)
            : await BuildFinalBundleAsync(
                template,
                sanitizedSubject,
                fullHtmlResult.Value,
                sanitizedBodyFragment,
                context,
                cancellationToken
            );
    }

    private async Task<RenderedNotificationBundle> BuildFinalBundleAsync(
        SystemCommunication template,
        string subject,
        string fullHtml,
        string bodyFragmentHtml,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken
    )
    {
        var smsBody = await RenderOptionalPlainAsync(
            template.SmsMessageTemplate,
            context,
            cancellationToken
        );
        var pushTitle = await RenderOptionalPlainAsync(
            template.PushTitleTemplate,
            context,
            cancellationToken
        );
        var pushBody = await RenderOptionalPlainAsync(
            template.PushMessageTemplate,
            context,
            cancellationToken
        );
        var pushUrl = await RenderOptionalPlainAsync(
            template.PushOpenUrlTemplate,
            context,
            cancellationToken
        );

        return new RenderedNotificationBundle(
            subject,
            fullHtml,
            StripHtmlForPlainFallback(bodyFragmentHtml),
            smsBody,
            pushTitle ?? subject,
            pushBody,
            pushUrl
        );
    }

    private Dictionary<string, object?> BuildMergeContext(
        string? recipientName,
        IReadOnlyDictionary<string, string?>? mergeFields
    )
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OrganizationName"] = _options.DefaultOrganizationName,
            ["RecipientName"] = string.IsNullOrWhiteSpace(recipientName)
                ? null
                : recipientName.Trim(),
        };

        if (mergeFields is not null)
        {
            foreach (var pair in mergeFields)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    dict[pair.Key.Trim()] = pair.Value;
                }
            }
        }

        return dict;
    }

    private async Task<Result<string>> WrapInRazorLayoutIfConfiguredAsync(
        SystemCommunication template,
        string renderedSubject,
        string renderedBodyHtml,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(template.RazorLayoutKey))
        {
            return renderedBodyHtml;
        }

        var orgName =
            context.TryGetValue("OrganizationName", out var rawOrg)
            && rawOrg is string orgText
            && !string.IsNullOrWhiteSpace(orgText)
                ? orgText
                : _options.DefaultOrganizationName;
        var actionLabel = context.TryGetValue("ActionLabel", out var rawLabel)
            ? rawLabel as string
            : null;
        var actionUrl = context.TryGetValue("ActionUrl", out var rawUrl) ? rawUrl as string : null;

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["OrganizationName"] = orgName,
            ["Subject"] = renderedSubject,
            ["Heading"] = template.Title,
            ["BodyHtml"] = renderedBodyHtml,
            ["ActionLabel"] = actionLabel,
            ["ActionUrl"] = actionUrl,
        };

        return await razorRenderer.RenderNamedTemplateAsync(
            template.RazorLayoutKey,
            parameters,
            cancellationToken
        );
    }

    private async Task<string?> RenderOptionalPlainAsync(
        string? markup,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            return null;
        }

        var res = await mergeEngine.RenderAsync(
            markup,
            context,
            MergeRenderOptions.DefaultPlainText,
            cancellationToken
        );
        return res.IsSuccess && !string.IsNullOrWhiteSpace(res.Value.RenderedContent)
            ? res.Value.RenderedContent.Trim()
            : null;
    }

    private static string StripHtmlForPlainFallback(string html)
    {
        var sb = new System.Text.StringBuilder(html.Length);
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
