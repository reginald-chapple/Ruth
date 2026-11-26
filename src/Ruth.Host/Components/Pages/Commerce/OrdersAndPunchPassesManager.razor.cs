using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Commerce;

/// <summary>Staff order fulfillment, refund, and issued punch pass balance ledger screen.</summary>
public sealed partial class OrdersAndPunchPassesManager : ComponentBase
{
    private IReadOnlyList<CommerceOrderResponse> _orders = [];
    private IReadOnlyList<IssuedPunchPassResponse> _passes = [];
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
        var ordRes = await QueryDispatcher.QueryAsync<
            GetCommerceOrdersQuery,
            IReadOnlyList<CommerceOrderResponse>
        >(new GetCommerceOrdersQuery());
        _orders = ordRes.IsSuccess ? ordRes.Value : [];

        var passRes = await QueryDispatcher.QueryAsync<
            GetIssuedPunchPassesQuery,
            IReadOnlyList<IssuedPunchPassResponse>
        >(new GetIssuedPunchPassesQuery());
        _passes = passRes.IsSuccess ? passRes.Value : [];
    }

    private async Task AdvanceAsync(Guid orderId, CommerceOrderStatus status)
    {
        var res = await CommandDispatcher.SendAsync<
            UpdateCommerceOrderFulfillmentCommand,
            CommerceOrderResponse
        >(new UpdateCommerceOrderFulfillmentCommand(orderId, status));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Updated order {res.Value.OrderNumber} to {res.Value.Status}.";
    }

    private async Task FullRefundAsync(Guid orderId, decimal amount)
    {
        var res = await CommandDispatcher.SendAsync<
            RefundCommerceOrderCommand,
            CommerceOrderResponse
        >(new RefundCommerceOrderCommand(orderId, amount, "Staff order manager refund"));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Refunded order {res.Value.OrderNumber}.";
    }

    private async Task RedeemOneCreditAsync(Guid passId)
    {
        var res = await CommandDispatcher.SendAsync<
            RedeemPunchPassCreditsCommand,
            IssuedPunchPassResponse
        >(new RedeemPunchPassCreditsCommand(passId, 1, ReferenceNotes: "Staff desk check-in"));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        await ReloadAsync();
        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Redeemed 1 credit on {res.Value.PassCode} ({res.Value.RemainingCredits} remaining; synced to Wallet Passes)."
        );
    }
}
