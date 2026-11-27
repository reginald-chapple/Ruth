using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FluentUI.AspNetCore.Components;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Pages.Tenancy;

/// <summary>Organization administrator compliance, custom domain, and Go-Live readiness studio.</summary>
public sealed partial class TenantComplianceSettings : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private ITenantContext TenantContext { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    internal TenantComplianceStatusResponse? Status { get; private set; }

    internal string NewCustomHostname { get; set; } = string.Empty;

    internal bool IsLoading { get; private set; } = true;

    internal bool IsAuthorizedAdmin { get; private set; }

    internal bool IsBusy { get; private set; }

    internal string? StatusBanner { get; private set; }

    internal MessageBarIntent BannerIntent { get; private set; } = MessageBarIntent.Info;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (!TenantContext.HasTenant)
            {
                return;
            }

            if (AuthenticationStateTask is not null)
            {
                var authState = await AuthenticationStateTask;
                var user = authState.User;
                if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Administrators"))
                {
                    return;
                }
            }

            IsAuthorizedAdmin = true;
            await ReloadAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    internal async Task ReloadAsync()
    {
        if (!IsAuthorizedAdmin)
        {
            return;
        }

        var result = await QueryDispatcher.QueryAsync<
            GetCurrentTenantComplianceQuery,
            TenantComplianceStatusResponse
        >(new GetCurrentTenantComplianceQuery());
        if (result.IsSuccess)
        {
            Status = result.Value;
            return;
        }

        StatusBanner = result.PrimaryError.Description;
        BannerIntent = MessageBarIntent.Error;
    }

    internal async Task CompleteMockStripeAsync()
    {
        if (!IsAuthorizedAdmin || Status is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (Status.StripeMockMode)
            {
                var res = await CommandDispatcher.SendAsync<
                    CompleteMockStripeConnectCommand,
                    TenantComplianceStatusResponse
                >(new CompleteMockStripeConnectCommand());
                ApplyStatusResult(res, "Stripe Connect payout readiness enabled.");
                return;
            }

            var returnUrl = Navigation.ToAbsoluteUri("/admin/compliance").ToString();
            var onboarding = await CommandDispatcher.SendAsync<
                StartStripeConnectOnboardingCommand,
                StripeConnectOnboardingResponse
            >(new StartStripeConnectOnboardingCommand(returnUrl, returnUrl));
            if (onboarding.IsFailure)
            {
                StatusBanner = onboarding.PrimaryError.Description;
                BannerIntent = MessageBarIntent.Error;
                return;
            }

            StatusBanner =
                $"Redirecting to Stripe Connect onboarding ({onboarding.Value.AccountId})...";
            BannerIntent = MessageBarIntent.Success;
            await ReloadAsync();
            Navigation.NavigateTo(onboarding.Value.OnboardingUrl, forceLoad: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task RegisterDomainAsync()
    {
        if (!IsAuthorizedAdmin || string.IsNullOrWhiteSpace(NewCustomHostname))
        {
            return;
        }

        IsBusy = true;
        try
        {
            var res = await CommandDispatcher.SendAsync<
                RegisterCustomDomainCommand,
                TenantDomainResponse
            >(
                new RegisterCustomDomainCommand(
                    NewCustomHostname.Trim(),
                    MakePrimaryWhenVerified: true
                )
            );
            StatusBanner = res.IsSuccess
                ? $"Registered {res.Value.Hostname}. Configure the DNS TXT token to verify ownership."
                : res.PrimaryError.Description;
            BannerIntent = res.IsSuccess ? MessageBarIntent.Success : MessageBarIntent.Error;
            NewCustomHostname = string.Empty;
            await ReloadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task VerifyDomainAsync(TenantDomainResponse domain)
    {
        if (!IsAuthorizedAdmin)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var observedToken = Status?.StripeMockMode == true ? domain.VerificationTxtValue : null;
            var res = await CommandDispatcher.SendAsync<
                VerifyCustomDomainCommand,
                TenantDomainResponse
            >(new VerifyCustomDomainCommand(domain.Id, observedToken));
            StatusBanner = res.IsSuccess
                ? $"Domain {res.Value.Hostname} verified."
                : res.PrimaryError.Description;
            BannerIntent = res.IsSuccess ? MessageBarIntent.Success : MessageBarIntent.Error;
            await ReloadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task SubmitForGoLiveAsync()
    {
        if (!IsAuthorizedAdmin)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var res = await CommandDispatcher.SendAsync<
                SubmitTenantForGoLiveCommand,
                TenantComplianceStatusResponse
            >(new SubmitTenantForGoLiveCommand());
            ApplyStatusResult(res, "Submitted for Platform Operator Go-Live approval.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyStatusResult(
        SharedKernel.Results.Result<TenantComplianceStatusResponse> result,
        string successMessage
    )
    {
        if (result.IsSuccess)
        {
            Status = result.Value;
            StatusBanner = successMessage;
            BannerIntent = MessageBarIntent.Success;
            return;
        }

        StatusBanner = string.Join(' ', result.Errors.Select(static e => e.Description));
        BannerIntent = MessageBarIntent.Error;
    }
}
