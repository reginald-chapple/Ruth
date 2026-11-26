using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Communication;

/// <summary>Staff workspace for multi-step automated drip campaign building, enrollment, and funnel analytics.</summary>
public sealed partial class CommunicationFlowsAndAnalytics : ComponentBase
{
    private List<CommunicationFlowResponse> _flows = [];
    private CommunicationFlowAnalyticsResponse? _analytics;

    private Guid _selectedFlowId;
    private string _cohortLabel = "New Member Wave";
    private string _enrollEmail = "newmember@example.org";
    private string _enrollName = "Taylor Brooks";

    private string _newFlowName = "Annual Renewal Drip";
    private string _newFlowKey = "annual-renewal-drip";
    private string _step1Subject = "Your Membership Renewal Window Is Open";
    private string _step2Subject = "Friendly Reminder: Renew Your Membership";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await RefreshFlowsAsync();
    }

    private async Task RefreshFlowsAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetCommunicationFlowsQuery,
            IReadOnlyList<CommunicationFlowResponse>
        >(new GetCommunicationFlowsQuery());
        if (res.IsSuccess)
        {
            _flows = [.. res.Value];
            if (_selectedFlowId == Guid.Empty && _flows.Count > 0)
            {
                _selectedFlowId = _flows[0].Id;
            }
        }

        if (_selectedFlowId != Guid.Empty)
        {
            await LoadAnalyticsAsync(_selectedFlowId);
        }
    }

    private async Task OnFlowSelectedAsync(Guid flowId)
    {
        _selectedFlowId = flowId;
        await LoadAnalyticsAsync(flowId);
    }

    private async Task LoadAnalyticsAsync(Guid flowId)
    {
        var analytics = await QueryDispatcher.QueryAsync<
            GetCommunicationFlowAnalyticsQuery,
            CommunicationFlowAnalyticsResponse
        >(new GetCommunicationFlowAnalyticsQuery(flowId));
        if (analytics.IsSuccess)
        {
            _analytics = analytics.Value;
        }
    }

    private async Task EnrollRecipientAsync()
    {
        if (_selectedFlowId == Guid.Empty)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            EnrollCommunicationFlowRecipientsCommand,
            CommunicationFlowInstanceResponse
        >(
            new EnrollCommunicationFlowRecipientsCommand(
                _selectedFlowId,
                [
                    new FlowRecipientEnrollmentInputDto(
                        EmailAddress: _enrollEmail,
                        DisplayName: _enrollName
                    ),
                ],
                CohortLabel: _cohortLabel
            )
        );

        if (res.IsSuccess)
        {
            _isError = false;
            _statusMessage = $"Enrolled into '{res.Value.CohortLabel}'.";
            await LoadAnalyticsAsync(_selectedFlowId);
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private async Task AdvanceDueStepsAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            ProcessDueCommunicationFlowsCommand,
            FlowAdvancementBatchResultResponse
        >(new ProcessDueCommunicationFlowsCommand(SpecificFlowId: _selectedFlowId));

        if (res.IsSuccess)
        {
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Advanced due steps: Sent={res.Value.StepsDispatched}, Completed={res.Value.RecipientsCompletedFlow}."
            );
            await LoadAnalyticsAsync(_selectedFlowId);
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private async Task CreateCustomFlowAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateCommunicationFlowCommand,
            CommunicationFlowResponse
        >(
            new CreateCommunicationFlowCommand(
                _newFlowName,
                _newFlowKey,
                CommunicationFlowCategory.MembershipAndRenewal,
                [
                    new CommunicationFlowStepDto(
                        0,
                        "step-1-open",
                        "Initial Notice",
                        CommunicationChannel.Email,
                        0,
                        _step1Subject,
                        "<p>Hello {{ RecipientName | default:'Member' }}, thank you for being part of our community.</p>"
                    ),
                    new CommunicationFlowStepDto(
                        1,
                        "step-2-reminder",
                        "Day 3 Reminder",
                        CommunicationChannel.Email,
                        4_320,
                        _step2Subject,
                        "<p>Just a friendly reminder to complete your renewal.</p>",
                        SendOnlyIfPreviousUnopened: true
                    ),
                ],
                TopicKey: "billing-and-dues"
            )
        );

        if (res.IsSuccess)
        {
            _selectedFlowId = res.Value.Id;
            _isError = false;
            _statusMessage = $"Created drip flow '{res.Value.Name}'.";
            await RefreshFlowsAsync();
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }
}
