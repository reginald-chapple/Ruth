using System.Globalization;
using System.Text;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Security;

namespace Ruth.Providers.CommunicationTransports.Smtp;

/// <summary>Constructs sanitized RFC 5322 / MIME multipart/alternative email messages for SMTP delivery.</summary>
internal static class SmtpMimeBuilder
{
    public static string BuildMimePayload(
        OutboundTransportMessage message,
        string fromAddress,
        string? fromDisplayName,
        DateTimeOffset utcNow
    )
    {
        var boundary = $"ruth-mime-{message.MessageId:N}";
        var sb = new StringBuilder(2048);
        var inv = CultureInfo.InvariantCulture;

        AppendHeader(sb, "Message-ID", $"<{message.MessageId:N}@ruth.local>");
        AppendHeader(sb, "Date", utcNow.ToString("R", inv));
        AppendHeader(sb, "From", FormatAddress(fromAddress, fromDisplayName));
        AppendHeader(sb, "To", FormatAddress(message.ToAddress, message.ToDisplayName));
        if (
            !string.IsNullOrWhiteSpace(message.ReplyToAddress)
            && EmailHeaderSanitizer.TrySanitizeEmailAddress(message.ReplyToAddress, out var replyTo)
        )
        {
            AppendHeader(sb, "Reply-To", $"<{replyTo}>");
        }

        var subject = EmailHeaderSanitizer.SanitizeHeaderValue(message.Subject);
        AppendHeader(sb, "Subject", string.IsNullOrEmpty(subject) ? "(No Subject)" : subject);
        AppendHeader(sb, "MIME-Version", "1.0");
        AppendHeader(sb, "Content-Type", $"multipart/alternative; boundary=\"{boundary}\"");
        _ = sb.Append("\r\n");

        var plain = message.PlainTextBody ?? StripTagsFallback(message.HtmlBody);
        AppendMimePart(sb, boundary, "text/plain; charset=utf-8", plain);
        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            AppendMimePart(sb, boundary, "text/html; charset=utf-8", message.HtmlBody);
        }

        _ = sb.Append("--").Append(boundary).Append("--\r\n");
        return sb.ToString();
    }

    public static string SanitizeHeader(string value)
    {
        return EmailHeaderSanitizer.SanitizeHeaderValue(value);
    }

    private static string FormatAddress(string email, string? displayName)
    {
        var cleanEmail = SanitizeHeader(email)
            .Replace("<", string.Empty, StringComparison.Ordinal)
            .Replace(">", string.Empty, StringComparison.Ordinal);
        var cleanName = SanitizeHeader(displayName ?? string.Empty);
        return string.IsNullOrWhiteSpace(cleanName)
            ? $"<{cleanEmail}>"
            : $"\"{cleanName.Replace('"', '\'')}\" <{cleanEmail}>";
    }

    private static void AppendHeader(StringBuilder sb, string name, string value)
    {
        _ = sb.Append(name).Append(": ").Append(SanitizeHeader(value)).Append("\r\n");
    }

    private static void AppendMimePart(
        StringBuilder sb,
        string boundary,
        string contentType,
        string content
    )
    {
        var base64 = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(content),
            Base64FormattingOptions.InsertLineBreaks
        );
        _ = sb.Append("--").Append(boundary).Append("\r\n");
        _ = sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
        _ = sb.Append("Content-Transfer-Encoding: base64\r\n\r\n");
        _ = sb.Append(base64).Append("\r\n");
    }

    private static string StripTagsFallback(string? html)
    {
        return string.IsNullOrWhiteSpace(html) ? string.Empty : html;
    }
}
