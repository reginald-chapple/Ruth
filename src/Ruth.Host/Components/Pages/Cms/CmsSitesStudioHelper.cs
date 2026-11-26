using System.Globalization;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Cms;

internal static class CmsSitesStudioHelper
{
    public static async Task<(bool Ok, string Message, string? QrSvg)> CreateShortLinkWithQrAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid siteId,
        string token,
        string targetUrl
    )
    {
        var res = await commands.SendAsync<CreatePageShortLinkCommand, PageShortLinkResponse>(
            new CreatePageShortLinkCommand(siteId, token, targetUrl)
        );
        if (res.IsFailure)
        {
            return (false, res.PrimaryError.Description, null);
        }

        var qrRes = await queries.QueryAsync<GetShortLinkQrQuery, ShortLinkQrResponse>(
            new GetShortLinkQrQuery(res.Value.Token)
        );
        return (
            true,
            $"Created shortlink /go/{res.Value.Token} with QR SVG.",
            qrRes.IsSuccess ? qrRes.Value.QrSvgMarkup : null
        );
    }

    public static async Task<(bool Ok, string Message, Guid? ThemeId)> CreateThemeAsync(
        ICommandDispatcher commands,
        string name,
        string slug,
        string accentColor
    )
    {
        var res = await commands.SendAsync<CreateCmsThemeCommand, CmsThemeResponse>(
            new CreateCmsThemeCommand(
                name,
                slug,
                CssCustomProperties: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["--cms-accent"] = accentColor,
                }
            )
        );
        return res.IsSuccess
            ? (true, $"Created theme '{res.Value.Slug}'.", res.Value.Id)
            : (false, res.PrimaryError.Description, null);
    }

    public static async Task<(bool Ok, string Message)> PublishHtmlVersionAsync(
        ICommandDispatcher commands,
        Guid blockId,
        string markup
    )
    {
        var res = await commands.SendAsync<SaveHtmlContentVersionCommand, HtmlContentResponse>(
            new SaveHtmlContentVersionCommand(blockId, markup, AutoApprove: true)
        );
        return res.IsSuccess
            ? (
                true,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Published approved HTML v{res.Value.Version}."
                )
            )
            : (false, res.PrimaryError.Description);
    }
}
