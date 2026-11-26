namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed record RenderedNotificationBundle(
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    string? SmsBody,
    string? PushTitle,
    string? PushBody,
    string? PushActionUrl
);
