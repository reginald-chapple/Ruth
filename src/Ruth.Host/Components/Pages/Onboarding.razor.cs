using Microsoft.AspNetCore.Components;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages;

/// <summary>First-run organization and initial administrator onboarding wizard page.</summary>
public sealed partial class Onboarding : ComponentBase
{
    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IOnboardingStateReader OnboardingStateReader { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    internal OnboardingFormModel Model { get; } = new();

    internal IReadOnlyList<TimeZoneOption> TimeZones { get; private set; } = [];

    internal bool IsSubmitting { get; private set; }

    internal string? ErrorMessage { get; private set; }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var status = await OnboardingStateReader.GetStatusAsync();
        if (!status.IsOnboardingRequired || status.IsSelfRegistrationLocked)
        {
            Navigation.NavigateTo("/", replace: true);
            return;
        }

        TimeZones =
        [
            .. TimeZoneInfo
                .GetSystemTimeZones()
                .Select(static tz => new TimeZoneOption(tz.Id, tz.DisplayName)),
        ];

        Model.TimeZoneId = ResolveDefaultTimeZoneId(TimeZones);
    }

    internal async Task SubmitAsync()
    {
        IsSubmitting = true;
        ErrorMessage = null;

        try
        {
            var shortCode = string.IsNullOrWhiteSpace(Model.PrimaryCampusShortCode)
                ? null
                : Model.PrimaryCampusShortCode.Trim();

            var command = new CompleteOnboardingCommand(
                Model.OrganizationName.Trim(),
                Model.TimeZoneId,
                Model.PrimaryCampusName.Trim(),
                shortCode,
                Model.AdminUserName.Trim(),
                Model.AdminEmail.Trim(),
                Model.AdminDisplayName.Trim(),
                Model.AdminPassword
            );

            var result = await CommandDispatcher.SendAsync<
                CompleteOnboardingCommand,
                CompleteOnboardingResponse
            >(command);

            if (result.IsSuccess)
            {
                OnboardingStateReader.InvalidateCache();
                Navigation.NavigateTo("/", forceLoad: true);
                return;
            }

            ErrorMessage = string.Join(
                ' ',
                result.Errors.Select(static error => error.Description)
            );
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private static string ResolveDefaultTimeZoneId(IReadOnlyList<TimeZoneOption> zones)
    {
        var localId = TimeZoneInfo.Local.Id;
        if (zones.Any(tz => string.Equals(tz.Id, localId, StringComparison.Ordinal)))
        {
            return localId;
        }

        var utcId = TimeZoneInfo.Utc.Id;
        return zones.Any(tz => string.Equals(tz.Id, utcId, StringComparison.Ordinal)) ? utcId
            : zones.Count > 0 ? zones[0].Id
            : "UTC";
    }
}
