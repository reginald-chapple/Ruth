using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>In-app Notification Bell inbox for Reminders, Support Care, and Following Digests.</summary>
public sealed partial class NotificationsInbox : ComponentBase
{
    private IReadOnlyList<NotificationMessageResponse> _notifications = [];

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetPersonNotificationsQuery,
            IReadOnlyList<NotificationMessageResponse>
        >(new GetPersonNotificationsQuery());
        _notifications = res.IsSuccess ? res.Value : [];
    }

    private async Task MarkReadAsync(Guid id)
    {
        _ = await CommandDispatcher.SendAsync<
            MarkNotificationReadCommand,
            NotificationMessageResponse
        >(new MarkNotificationReadCommand(id));
        await ReloadAsync();
    }
}
