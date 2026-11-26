using Microsoft.AspNetCore.Components;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Pages.Tenancy;

/// <summary>Internal Platform Operator console strictly gated to the dedicated operator hostname.</summary>
public sealed partial class PlatformOperatorConsole : ComponentBase
{
    [Inject]
    private ITenantContext TenantContext { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    internal PlatformOperatorAuthResponse? OperatorSession { get; private set; }

    internal IReadOnlyList<TenantSummaryResponse> Queue { get; private set; } = [];

    internal string OperatorEmail { get; set; } = string.Empty;

    internal string OperatorPassword { get; set; } = string.Empty;

    internal bool IsBusy { get; private set; }

    internal string? FeedbackMessage { get; private set; }

    internal bool IsOperatorHost => TenantContext.IsPlatformOperatorHost;

    internal async Task SignInOperatorAsync()
    {
        if (!IsOperatorHost)
        {
            return;
        }

        IsBusy = true;
        FeedbackMessage = null;
        try
        {
            var result = await CommandDispatcher.SendAsync<
                AuthenticatePlatformOperatorCommand,
                PlatformOperatorAuthResponse
            >(new AuthenticatePlatformOperatorCommand(OperatorEmail.Trim(), OperatorPassword));
            if (result.IsFailure)
            {
                FeedbackMessage = result.PrimaryError.Description;
                return;
            }

            OperatorSession = result.Value;
            OperatorPassword = string.Empty;
            await LoadQueueAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    internal async Task LoadQueueAsync()
    {
        var queue = await QueryDispatcher.QueryAsync<
            GetPendingGoLiveQueueQuery,
            IReadOnlyList<TenantSummaryResponse>
        >(new GetPendingGoLiveQueueQuery(TenantLifecycleStage.PendingVerification));
        if (queue.IsSuccess)
        {
            Queue = queue.Value;
        }
    }

    internal async Task ReviewAsync(Guid tenantId, OperatorReviewDecision decision)
    {
        if (OperatorSession is null || !IsOperatorHost)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var res = await CommandDispatcher.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId,
                    decision,
                    OperatorSession.Email,
                    $"Reviewed via Operator Console ({decision})"
                )
            );
            FeedbackMessage = res.IsSuccess
                ? $"Updated {res.Value.LegalName} to {res.Value.LifecycleStage}."
                : res.PrimaryError.Description;
            await LoadQueueAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
