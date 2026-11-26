using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Following;
using Ruth.Modules.Engagement.Contracts.Reminders;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Staff Care-Team Moderation Queue, Entity Reminders, and Following Digest workspace for W5-S3.</summary>
public sealed partial class CareRemindersAndFollowing : ComponentBase
{
    private IReadOnlyList<SupportRequestResponse> _queue = [];
    private IReadOnlyList<ReminderResponse> _reminders = [];

    private string _categoryName = "Member Wellbeing & Assistance";
    private string _categorySlug = "member-wellbeing-assistance";
    private string _reminderTypeName = "Care Follow-Up Check-In";

    private string? _statusMessage;
    private bool _isError;

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
        var qRes = await QueryDispatcher.QueryAsync<
            GetCareTeamReviewQueueQuery,
            IReadOnlyList<SupportRequestResponse>
        >(new GetCareTeamReviewQueueQuery());
        _queue = qRes.IsSuccess ? qRes.Value : [];

        var rRes = await QueryDispatcher.QueryAsync<
            GetRemindersQuery,
            IReadOnlyList<ReminderResponse>
        >(new GetRemindersQuery());
        _reminders = rRes.IsSuccess ? rRes.Value : [];
    }

    private async Task CreateCategoryAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateSupportCategoryCommand,
            SupportCategoryResponse
        >(new CreateSupportCategoryCommand(new SupportCategorySpec(_categoryName, _categorySlug)));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created support category '{res.Value.Name}'."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task CreateReminderTypeAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateReminderTypeCommand,
            ReminderTypeResponse
        >(new CreateReminderTypeCommand(new ReminderTypeSpec(_reminderTypeName)));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created reminder type '{res.Value.Name}'."
            : res.PrimaryError.Description;
    }

    private async Task ModerateAsync(Guid id, SupportModerationDecision decision)
    {
        var res = await CommandDispatcher.SendAsync<
            ModerateSupportRequestCommand,
            SupportRequestResponse
        >(new ModerateSupportRequestCommand(id, decision));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Applied moderation: {decision}."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task SupportCareTeamAsync(Guid id)
    {
        _ = await CommandDispatcher.SendAsync<
            RecordCommunitySupportCommand,
            SupportRequestResponse
        >(new RecordCommunitySupportCommand(id, IsCareTeamMember: true));
        await ReloadAsync();
    }

    private async Task CompleteReminderAsync(Guid id)
    {
        _ = await CommandDispatcher.SendAsync<CompleteReminderCommand, ReminderResponse>(
            new CompleteReminderCommand(id)
        );
        await ReloadAsync();
    }

    private async Task RunRemindersAndDigestsAsync()
    {
        var rem = await CommandDispatcher.SendAsync<
            ProcessDueRemindersCommand,
            ReminderProcessRunResponse
        >(new ProcessDueRemindersCommand());
        var dig = await CommandDispatcher.SendAsync<
            DispatchFollowingDigestsCommand,
            FollowingDigestRunResponse
        >(new DispatchFollowingDigestsCommand());
        _isError = rem.IsFailure || dig.IsFailure;
        var remCount = rem.IsSuccess ? rem.Value.NotifiedCount : 0;
        var digCount = dig.IsSuccess ? dig.Value.DigestsSentCount : 0;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Processed {remCount} due reminders and {digCount} following digests."
        );
        await ReloadAsync();
    }
}
