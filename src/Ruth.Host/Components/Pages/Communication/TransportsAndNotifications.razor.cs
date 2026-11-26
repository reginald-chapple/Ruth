using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Communication;

/// <summary>Admin UI for inspecting communication transports, E.164 phone numbers, and system notifications.</summary>
public sealed partial class TransportsAndNotifications : ComponentBase
{
    private List<CommunicationTransportStatusResponse> _transports = [];
    private List<SystemCommunicationResponse> _systemCommunications = [];
    private List<SystemPhoneNumberResponse> _phoneNumbers = [];
    private List<SystemNotificationLogResponse> _logs = [];

    private string _selectedKey = "identity.password-reset";
    private string _testEmail = "member@example.org";
    private string _testPhone = "+15552345678";
    private string _testRecipientName = "Jordan Taylor";

    private string _phoneName = "Main Support SMS";
    private string _phoneNumber = "+15559876543";
    private bool _phoneSmsEnabled = true;

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        var transports = await QueryDispatcher.QueryAsync<
            GetCommunicationTransportsQuery,
            IReadOnlyList<CommunicationTransportStatusResponse>
        >(new GetCommunicationTransportsQuery());
        if (transports.IsSuccess)
        {
            _transports = [.. transports.Value];
        }

        var comms = await QueryDispatcher.QueryAsync<
            GetSystemCommunicationsQuery,
            IReadOnlyList<SystemCommunicationResponse>
        >(new GetSystemCommunicationsQuery());
        if (comms.IsSuccess)
        {
            _systemCommunications = [.. comms.Value];
            if (_systemCommunications.Count > 0 && string.IsNullOrWhiteSpace(_selectedKey))
            {
                _selectedKey = _systemCommunications[0].Key;
            }
        }

        var phones = await QueryDispatcher.QueryAsync<
            GetSystemPhoneNumbersQuery,
            IReadOnlyList<SystemPhoneNumberResponse>
        >(new GetSystemPhoneNumbersQuery());
        if (phones.IsSuccess)
        {
            _phoneNumbers = [.. phones.Value];
        }

        var logs = await QueryDispatcher.QueryAsync<
            GetSystemNotificationLogsQuery,
            IReadOnlyList<SystemNotificationLogResponse>
        >(new GetSystemNotificationLogsQuery(Limit: 25));
        if (logs.IsSuccess)
        {
            _logs = [.. logs.Value];
        }
    }

    private async Task SendTestNotificationAsync()
    {
        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = "jordan.taylor",
            ["ResetToken"] = "DEMO-948210",
            ["ConfirmationToken"] = "CONF-441029",
            ["Amount"] = "$125.00",
            ["ReceiptNumber"] = "RCPT-2026-001",
            ["TaskTitle"] = "Review New Volunteer Application",
        };

        var result = await CommandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                _selectedKey,
                string.IsNullOrWhiteSpace(_testEmail) ? null : _testEmail,
                _testRecipientName,
                string.IsNullOrWhiteSpace(_testPhone) ? null : _testPhone,
                MergeFields: merge,
                TriggerSource: "AdminUI.TestSend"
            )
        );

        if (result.IsSuccess)
        {
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Dispatched {result.Value.DispatchedCount} notification(s) for '{result.Value.SystemCommunicationKey}'."
            );
            await RefreshAllAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task CreatePhoneNumberAsync()
    {
        var result = await CommandDispatcher.SendAsync<
            CreateSystemPhoneNumberCommand,
            SystemPhoneNumberResponse
        >(
            new CreateSystemPhoneNumberCommand(
                _phoneName,
                _phoneNumber,
                IsSmsEnabled: _phoneSmsEnabled
            )
        );

        if (result.IsSuccess)
        {
            _isError = false;
            _statusMessage = $"Registered E.164 phone number {result.Value.Number}.";
            await RefreshAllAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }
}
