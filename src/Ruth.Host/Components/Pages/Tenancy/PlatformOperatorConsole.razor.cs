using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Ruth.Modules.Tenancy.Contracts.Operators;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.Components.Pages.Tenancy;

/// <summary>Internal Platform Operator console strictly gated to the dedicated operator hostname.</summary>
public sealed partial class PlatformOperatorConsole : ComponentBase
{
    internal static readonly string[] StageFilterOptions =
    [
        "PendingVerification",
        "Sandbox",
        "Live",
        "Suspended",
        "All",
    ];

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

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

    internal string SelectedStageFilter { get; private set; } = "PendingVerification";

    internal string ReviewNotes { get; set; } = string.Empty;

    internal bool ManualIrsOverrideApproved { get; set; }

    internal bool IsBusy { get; private set; }

    internal string? FeedbackMessage { get; private set; }

    internal bool IsOperatorHost => TenantContext.IsPlatformOperatorHost;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (!IsOperatorHost || AuthenticationStateTask is null)
        {
            return;
        }

        var authState = await AuthenticationStateTask;
        var user = authState.User;
        if (user.Identity?.IsAuthenticated == true && user.HasClaim("platform_operator", "true"))
        {
            var idStr = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = user.FindFirstValue(ClaimTypes.Email);
            var displayName = user.FindFirstValue("display_name") ?? email ?? "Platform Operator";
            if (!string.IsNullOrWhiteSpace(email))
            {
                _ = Guid.TryParse(idStr, out var operatorId);
                OperatorSession = new PlatformOperatorAuthResponse(operatorId, email, displayName);
                await LoadQueueAsync();
            }
        }
    }

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

    internal void SignOutOperator()
    {
        OperatorSession = null;
        Queue = [];
        FeedbackMessage = null;
    }

    internal async Task OnStageFilterChangedAsync(string? stage)
    {
        SelectedStageFilter = string.IsNullOrWhiteSpace(stage) ? "PendingVerification" : stage;
        await LoadQueueAsync();
    }

    internal async Task LoadQueueAsync()
    {
        TenantLifecycleStage? parsedStage = Enum.TryParse<TenantLifecycleStage>(
            SelectedStageFilter,
            ignoreCase: true,
            out var stage
        )
            ? stage
            : null;

        var queue = await QueryDispatcher.QueryAsync<
            GetPendingGoLiveQueueQuery,
            IReadOnlyList<TenantSummaryResponse>
        >(new GetPendingGoLiveQueueQuery(parsedStage));
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
            var effectiveNotes = string.IsNullOrWhiteSpace(ReviewNotes)
                ? $"Reviewed via Operator Console ({decision})"
                : ReviewNotes.Trim();

            var res = await CommandDispatcher.SendAsync<
                ReviewTenantGoLiveCommand,
                TenantComplianceStatusResponse
            >(
                new ReviewTenantGoLiveCommand(
                    tenantId,
                    decision,
                    OperatorSession.Email,
                    effectiveNotes,
                    ManualIrsOverrideApproved
                )
            );
            FeedbackMessage = res.IsSuccess
                ? $"Updated {res.Value.LegalName} to {res.Value.LifecycleStage}."
                : res.PrimaryError.Description;
            if (res.IsSuccess)
            {
                ReviewNotes = string.Empty;
                ManualIrsOverrideApproved = false;
            }

            await LoadQueueAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
