using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Pages.Tenancy;

/// <summary>Organization administrator compliance, custom domain, and Go-Live readiness studio.</summary>
public sealed partial class TenantComplianceSettings : ComponentBase
{
    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private ITenantContext TenantContext { get; set; } = default!;

    internal TenantComplianceStatusResponse? Status { get; private set; }

    internal string NewCustomHostname { get; set; } = string.Empty;

    internal bool IsBusy { get; private set; }

    internal string? StatusBanner { get; private set; }

    internal MessageBarIntent BannerIntent { get; private set; } = MessageBarIntent.Info;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (!TenantContext.HasTenant)
        {
            return;
        }

        await ReloadAsync();
    }

    internal async Task ReloadAsync()
    {
        var result = await QueryDispatcher.QueryAsync<
            GetCurrentTenantComplianceQuery,
            TenantComplianceStatusResponse
        >(new GetCurrentTenantComplianceQuery());
        if (result.IsSuccess)
        {
            Status = result.Value;
        }
    }

    internal async Task CompleteMockStripeAsync()
    {
        if (Status is null)
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

            var onboarding = await CommandDispatcher.SendAsync<
                StartStripeConnectOnboardingCommand,
                StripeConnectOnboardingResponse
            >(new StartStripeConnectOnboardingCommand("/admin/compliance", "/admin/compliance"));
            StatusBanner = onboarding.IsSuccess
                ? $"Stripe Connect account {onboarding.Value.AccountId} created."
                : onboarding.PrimaryError.Description;
            BannerIntent = onboarding.IsSuccess ? MessageBarIntent.Success : MessageBarIntent.Error;
            await ReloadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task RegisterDomainAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCustomHostname))
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
        IsBusy = true;
        try
        {
            var res = await CommandDispatcher.SendAsync<
                VerifyCustomDomainCommand,
                TenantDomainResponse
            >(
                new VerifyCustomDomainCommand(
                    domain.Id,
                    domain.VerificationTxtValue ?? string.Empty
                )
            );
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
