using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Host.Components.Pages.Commerce;

/// <summary>Multi-step public Booking Wizard for 1-on-1 services, virtual workshops, and facility/resource rentals.</summary>
public sealed partial class PublicBookingWizard : ComponentBase
{
    private IReadOnlyList<BookableServiceResponse> _services = [];
    private IReadOnlyList<RentableResourceResponse> _resources = [];
    private IReadOnlyList<AvailableBookingSlotDto> _availableSlots = [];

    private BookingTargetKind _selectedKind = BookingTargetKind.Service;
    private string _selectedSlug = string.Empty;
    private BookingSlotHoldResponse? _activeHold;
    private BookingQuoteResponse? _quote;
    private ServiceBookingResponse? _confirmedBooking;

    private string _customerName = "Morgan Lee";
    private string _customerEmail = "morgan.lee@example.org";
    private string _memberTierInput = string.Empty;
    private string _paymentTokenInput = "tok_booking_card_4242";

    private string? _statusMessage;
    private bool _isError;

    [Parameter]
    public string? Slug { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        var svcRes = await QueryDispatcher.QueryAsync<
            GetBookableServicesQuery,
            IReadOnlyList<BookableServiceResponse>
        >(new GetBookableServicesQuery(ActiveOnly: true, PublicOnly: true));
        _services = svcRes.IsSuccess ? svcRes.Value : [];

        var resRes = await QueryDispatcher.QueryAsync<
            GetRentableResourcesQuery,
            IReadOnlyList<RentableResourceResponse>
        >(new GetRentableResourcesQuery(ActiveOnly: true, PublicOnly: true));
        _resources = resRes.IsSuccess ? resRes.Value : [];

        if (!string.IsNullOrWhiteSpace(Slug))
        {
            var matchedService = _services.Any(s =>
                string.Equals(s.Slug, Slug, StringComparison.OrdinalIgnoreCase)
            );
            await SelectTargetAsync(
                matchedService ? BookingTargetKind.Service : BookingTargetKind.Resource,
                Slug
            );
        }
    }

    private async Task SelectTargetAsync(BookingTargetKind kind, string slug)
    {
        _selectedKind = kind;
        _selectedSlug = slug;
        _activeHold = null;
        _quote = null;

        var start = Clock.UtcNow.AddHours(2);
        var slotsRes = await QueryDispatcher.QueryAsync<
            QueryAvailableBookingSlotsQuery,
            IReadOnlyList<AvailableBookingSlotDto>
        >(new QueryAvailableBookingSlotsQuery(kind, slug, start, start.AddDays(7)));

        if (slotsRes.IsFailure)
        {
            _isError = true;
            _statusMessage = slotsRes.PrimaryError.Description;
            _availableSlots = [];
            return;
        }

        _availableSlots = slotsRes.Value;
        _isError = false;
        _statusMessage =
            _availableSlots.Count == 0
                ? "No open slots found in the next 7 days."
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"Found {_availableSlots.Count} conflict-free available slot(s)."
                );
    }

    private async Task HoldSlotAsync(AvailableBookingSlotDto slot)
    {
        var holdRes = await CommandDispatcher.SendAsync<
            CreateBookingSlotHoldCommand,
            BookingSlotHoldResponse
        >(
            new CreateBookingSlotHoldCommand(
                _selectedKind,
                _selectedSlug,
                slot.SlotStartUtc,
                ProviderPersonId: slot.ProviderPersonId
            )
        );
        if (holdRes.IsFailure)
        {
            _isError = true;
            _statusMessage = holdRes.PrimaryError.Description;
            return;
        }

        _activeHold = holdRes.Value;
        await RefreshQuoteAsync();
    }

    private async Task RefreshQuoteAsync()
    {
        if (_activeHold is null)
        {
            return;
        }

        var quoteRes = await QueryDispatcher.QueryAsync<
            PreviewBookingQuoteQuery,
            BookingQuoteResponse
        >(
            new PreviewBookingQuoteQuery(
                _selectedKind,
                _selectedSlug,
                _activeHold.SlotStartUtc,
                ExplicitMembershipTier: string.IsNullOrWhiteSpace(_memberTierInput)
                    ? null
                    : _memberTierInput
            )
        );
        if (quoteRes.IsFailure)
        {
            _isError = true;
            _statusMessage = quoteRes.PrimaryError.Description;
            return;
        }

        _quote = quoteRes.Value;
        _isError = false;
    }

    private async Task ConfirmBookingAsync()
    {
        if (_activeHold is null || _quote is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<ConfirmBookingCommand, ServiceBookingResponse>(
            new ConfirmBookingCommand(
                _selectedKind,
                _selectedSlug,
                _activeHold.SlotStartUtc,
                _customerName,
                _customerEmail,
                ProviderPersonId: _activeHold.ProviderPersonId,
                HoldToken: _activeHold.HoldToken,
                ExplicitMembershipTier: string.IsNullOrWhiteSpace(_memberTierInput)
                    ? null
                    : _memberTierInput,
                OneTimeHostedPaymentToken: _quote.TotalAmount > 0m ? _paymentTokenInput : null
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _confirmedBooking = res.Value;
        _isError = false;
        _statusMessage = $"Confirmed booking {res.Value.ConfirmationCode}.";
    }
}
