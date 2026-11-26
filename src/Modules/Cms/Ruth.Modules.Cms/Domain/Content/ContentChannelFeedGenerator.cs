using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Clean-room RSS 2.0 XML and JSON Feed 1.1 generator for CMS ContentChannels.</summary>
public static class ContentChannelFeedGenerator
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static string GenerateRss2(
        ContentChannel channel,
        IReadOnlyList<ContentChannelItem> publishedItems,
        string baseOrigin
    )
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(publishedItems);

        var origin = NormalizeOrigin(baseOrigin);
        var channelLink = BuildChannelLink(channel, origin);

        var sb = new StringBuilder();
        using (
            var writer = XmlWriter.Create(
                sb,
                new XmlWriterSettings { OmitXmlDeclaration = false, Indent = true }
            )
        )
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteStartElement("channel");

            writer.WriteElementString("title", channel.Name);
            writer.WriteElementString("link", channelLink);
            writer.WriteElementString("description", channel.Description ?? channel.Name);
            writer.WriteElementString(
                "ttl",
                channel.TimeToLiveMinutes.ToString(CultureInfo.InvariantCulture)
            );

            foreach (var item in publishedItems)
            {
                writer.WriteStartElement("item");
                writer.WriteElementString("title", item.Title);
                writer.WriteElementString("link", BuildItemLink(channel, item, origin));
                writer.WriteElementString("guid", item.Id.Value.ToString("D"));
                writer.WriteElementString("description", item.Summary ?? item.Content);
                writer.WriteElementString(
                    "pubDate",
                    item.StartDateTimeUtc.ToUniversalTime()
                        .ToString("R", CultureInfo.InvariantCulture)
                );
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return sb.ToString();
    }

    public static string GenerateJsonFeed11(
        ContentChannel channel,
        IReadOnlyList<ContentChannelItem> publishedItems,
        string baseOrigin
    )
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(publishedItems);

        var origin = NormalizeOrigin(baseOrigin);
        var feed = new
        {
            version = "https://jsonfeed.org/version/1.1",
            title = channel.Name,
            home_page_url = BuildChannelLink(channel, origin),
            feed_url = $"{origin}/api/cms/channels/{channel.Slug}/feed.json",
            description = channel.Description ?? channel.Name,
            items = publishedItems.Select(item => new
            {
                id = item.Id.Value.ToString("D"),
                url = BuildItemLink(channel, item, origin),
                title = item.Title,
                summary = item.Summary,
                content_html = item.Content,
                date_published = item.StartDateTimeUtc.ToUniversalTime().ToString("O"),
            }),
        };

        return JsonSerializer.Serialize(feed, _jsonOptions);
    }

    private static string NormalizeOrigin(string baseOrigin)
    {
        return string.IsNullOrWhiteSpace(baseOrigin)
            ? "https://ruth.local"
            : baseOrigin.Trim().TrimEnd('/');
    }

    private static string BuildChannelLink(ContentChannel channel, string origin)
    {
        return string.IsNullOrWhiteSpace(channel.ChannelUrl)
            ? $"{origin}/cms/channels/{channel.Slug}"
            : channel.ChannelUrl;
    }

    private static string BuildItemLink(
        ContentChannel channel,
        ContentChannelItem item,
        string origin
    )
    {
        return !string.IsNullOrWhiteSpace(channel.ItemUrl)
            ? channel
                .ItemUrl.Replace("{Slug}", item.Slug, StringComparison.OrdinalIgnoreCase)
                .Replace("{Id}", item.Id.Value.ToString("D"), StringComparison.OrdinalIgnoreCase)
            : $"{origin}/cms/channels/{channel.Slug}/{item.Slug}";
    }
}
