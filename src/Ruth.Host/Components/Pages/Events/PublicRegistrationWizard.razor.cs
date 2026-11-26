using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Events;

/// <summary>Multi-step public Event Registration Wizard with TTL seat holds, dynamic form fields, discounts, and deposit/full checkout.</summary>
public sealed partial class PublicRegistrationWizard : ComponentBase
{
    private PublicRegistrationWizardInitResponse? _wizard;
    private RegistrationSessionResponse? _sessionHold;
    private RegistrationPricingQuoteResponse? _quote;
    private RegistrationResponse? _completedRegistration;

    private int _step = 1;
    private string _seatCountInput = "1";
    private string _registrarFirstName = "Jordan";
    private string _registrarLastName = "Taylor";
    private string _registrarEmail = "jordan.taylor@example.org";
    private readonly Dictionary<string, string?> _fieldValues = new(
        StringComparer.OrdinalIgnoreCase
    );
    private string _discountCode = string.Empty;
    private string _paymentAmountInput = "0.00";
    private string _paymentTokenInput = "tok_wizard_card_4242";

    private string? _statusMessage;
    private bool _isError;

    [Parameter]
    public string Slug { get; set; } = string.Empty;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetRegistrationInstancePortalQuery,
            PublicRegistrationWizardInitResponse
        >(new GetRegistrationInstancePortalQuery(Slug));
        _wizard = res.IsSuccess ? res.Value : null;
    }

    private string GetPrimaryFieldValue(string key)
    {
        return _fieldValues.TryGetValue(key, out var val) && val is not null ? val : string.Empty;
    }

    private void SetPrimaryFieldValue(string key, string? value)
    {
        _fieldValues[key] = value;
    }

    private async Task ReserveSeatsAsync()
    {
        if (
            _wizard is null
            || !int.TryParse(
                _seatCountInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var count
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid attendee seat count.";
            return;
        }

        var holdRes = await CommandDispatcher.SendAsync<
            CreateRegistrationSessionHoldCommand,
            RegistrationSessionResponse
        >(new CreateRegistrationSessionHoldCommand(_wizard.Instance.Slug, count));
        if (holdRes.IsFailure)
        {
            _isError = true;
            _statusMessage = holdRes.PrimaryError.Description;
            return;
        }

        _sessionHold = holdRes.Value;
        _step = 2;
        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Reserved {holdRes.Value.HeldConfirmedSeats} confirmed and {holdRes.Value.HeldWaitlistSeats} waitlist seat(s) until {holdRes.Value.ExpiresAtUtc:HH:mm} UTC."
        );
    }

    private async Task AdvanceToPricingAsync()
    {
        await RefreshQuoteAsync();
        if (!_isError)
        {
            _step = 3;
        }
    }

    private async Task RefreshQuoteAsync()
    {
        if (_wizard is null)
        {
            return;
        }

        var submissions = BuildRegistrantSubmissions();
        var quoteRes = await QueryDispatcher.QueryAsync<
            PreviewRegistrationPricingQuery,
            RegistrationPricingQuoteResponse
        >(
            new PreviewRegistrationPricingQuery(
                _wizard.Instance.Slug,
                submissions,
                string.IsNullOrWhiteSpace(_discountCode) ? null : _discountCode,
                _sessionHold?.SessionToken
            )
        );
        if (quoteRes.IsFailure)
        {
            _isError = true;
            _statusMessage = quoteRes.PrimaryError.Description;
            return;
        }

        _quote = quoteRes.Value;
        _paymentAmountInput = _quote.MinimumInitialAmountDue.ToString(
            "F2",
            CultureInfo.InvariantCulture
        );
        _isError = false;
    }

    private async Task SubmitWizardAsync()
    {
        if (
            _wizard is null
            || !decimal.TryParse(
                _paymentAmountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var payAmt
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid payment amount.";
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            SubmitRegistrationCommand,
            RegistrationResponse
        >(
            new SubmitRegistrationCommand(
                _wizard.Instance.Slug,
                _registrarFirstName,
                _registrarLastName,
                _registrarEmail,
                BuildRegistrantSubmissions(),
                SessionToken: _sessionHold?.SessionToken,
                DiscountCode: string.IsNullOrWhiteSpace(_discountCode) ? null : _discountCode,
                InitialPaymentAmount: payAmt,
                OneTimeHostedPaymentToken: payAmt > 0m ? _paymentTokenInput : null
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _completedRegistration = res.Value;
        _isError = false;
        _statusMessage = $"Registration completed ({res.Value.ConfirmationCode}).";
    }

    private RegistrantSubmissionDto[] BuildRegistrantSubmissions()
    {
        var count = _sessionHold?.RequestedSeatCount ?? 1;
        var list = new RegistrantSubmissionDto[count];
        for (var i = 0; i < count; i++)
        {
            var suffix =
                i == 0 ? string.Empty : string.Create(CultureInfo.InvariantCulture, $" #{i + 1}");
            list[i] = new RegistrantSubmissionDto(
                _registrarFirstName,
                $"{_registrarLastName}{suffix}",
                _registrarEmail,
                FieldValues: new Dictionary<string, string?>(
                    _fieldValues,
                    StringComparer.OrdinalIgnoreCase
                )
            );
        }

        return list;
    }
}
