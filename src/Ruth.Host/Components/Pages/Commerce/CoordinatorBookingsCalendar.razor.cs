using Microsoft.AspNetCore.Components;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Commerce;

/// <summary>Staff Coordinator Calendar and setup screen for BookableServices, RentableResources, and AvailabilityRules.</summary>
public sealed partial class CoordinatorBookingsCalendar : ComponentBase
{
    private static readonly BookingDeliveryMode[] _deliveryModes =
    [
        BookingDeliveryMode.Remote,
        BookingDeliveryMode.InPerson,
        BookingDeliveryMode.Hybrid,
    ];

    private IReadOnlyList<FinancialAccountResponse> _accounts = [];
    private IReadOnlyList<BookableServiceResponse> _services = [];
    private IReadOnlyList<RentableResourceResponse> _resources = [];
    private IReadOnlyList<ServiceBookingResponse> _bookings = [];

    private string _serviceName = "1-on-1 Member Coaching Session";
    private string _serviceSlug = "member-coaching-session";
    private BookingDeliveryMode _deliveryMode = BookingDeliveryMode.Remote;
    private Guid _revenueAccountId;
    private string _serviceDurationInput = "45";
    private string _servicePriceInput = "60.00";
    private string _memberTierFreeInput = "Gold";

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
        var accRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true));
        if (accRes.IsSuccess)
        {
            _accounts = accRes.Value;
            if (_revenueAccountId == Guid.Empty && _accounts.Count > 0)
            {
                _revenueAccountId = _accounts[0].Id;
            }
        }

        var svcRes = await QueryDispatcher.QueryAsync<
            GetBookableServicesQuery,
            IReadOnlyList<BookableServiceResponse>
        >(new GetBookableServicesQuery());
        _services = svcRes.IsSuccess ? svcRes.Value : [];

        var resRes = await QueryDispatcher.QueryAsync<
            GetRentableResourcesQuery,
            IReadOnlyList<RentableResourceResponse>
        >(new GetRentableResourcesQuery());
        _resources = resRes.IsSuccess ? resRes.Value : [];

        var bkgRes = await QueryDispatcher.QueryAsync<
            GetServiceBookingsQuery,
            IReadOnlyList<ServiceBookingResponse>
        >(new GetServiceBookingsQuery());
        _bookings = bkgRes.IsSuccess ? bkgRes.Value : [];
    }

    private async Task CreateServiceAsync()
    {
        if (
            !CommerceBookingSpecFactory.TryBuildServiceSpec(
                _serviceName,
                _serviceSlug,
                _deliveryMode,
                _revenueAccountId,
                _serviceDurationInput,
                _servicePriceInput,
                _memberTierFreeInput,
                out var spec
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter valid numeric duration and standard price.";
            return;
        }

        var svcRes = await CommandDispatcher.SendAsync<
            CreateBookableServiceCommand,
            BookableServiceResponse
        >(new CreateBookableServiceCommand(spec!));
        if (svcRes.IsFailure)
        {
            _isError = true;
            _statusMessage = svcRes.PrimaryError.Description;
            return;
        }

        foreach (
            var ruleCmd in CommerceBookingSpecFactory.BuildDefaultWeekdayRules(svcRes.Value.Id)
        )
        {
            _ = await CommandDispatcher.SendAsync<
                CreateAvailabilityRuleCommand,
                AvailabilityRuleResponse
            >(ruleCmd);
        }

        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Created service {svcRes.Value.Name} with weekday availability windows.";
    }

    private async Task CoordinatorCancelAsync(Guid bookingId)
    {
        var res = await CommandDispatcher.SendAsync<CancelBookingCommand, ServiceBookingResponse>(
            new CancelBookingCommand(
                bookingId,
                "Coordinator calendar cancellation",
                StaffOverride: true
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Cancelled booking {res.Value.ConfirmationCode}.";
    }
}
