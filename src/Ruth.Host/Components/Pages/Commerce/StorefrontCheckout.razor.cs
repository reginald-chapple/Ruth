using Microsoft.AspNetCore.Components;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Commerce;

/// <summary>Public storefront catalog, automatic member pricing quote preview, and IRS quid-pro-quo checkout UI.</summary>
public sealed partial class StorefrontCheckout : ComponentBase
{
    private static readonly CommerceFulfillmentMethod[] _fulfillmentMethods =
    [
        CommerceFulfillmentMethod.LocalPickup,
        CommerceFulfillmentMethod.FlatRateShipping,
        CommerceFulfillmentMethod.DigitalOrInstant,
    ];

    private readonly Dictionary<Guid, int> _cart = [];
    private IReadOnlyList<ProductResponse> _products = [];
    private CommerceOrderQuoteResponse? _quote;
    private CommerceOrderResponse? _completedOrder;
    private string? _downloadUrl;

    private string _buyerName = "Alex Kim";
    private string _buyerEmail = "alex.kim@example.org";
    private string _memberTierInput = string.Empty;
    private CommerceFulfillmentMethod _fulfillmentMethod = CommerceFulfillmentMethod.LocalPickup;
    private string _shippingAddress = "100 Main St, Denver, CO 80202";
    private string _paymentToken = "tok_storefront_card_4242";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetProductsQuery,
            IReadOnlyList<ProductResponse>
        >(new GetProductsQuery(ActiveOnly: true, PublicOnly: true));
        _products = res.IsSuccess ? res.Value : [];
    }

    private async Task AddToCartAsync(Guid variantId)
    {
        _cart[variantId] = _cart.GetValueOrDefault(variantId) + 1;
        await RefreshQuoteAsync();
    }

    private async Task RefreshQuoteAsync()
    {
        CommerceCartItemDto[] items =
        [
            .. _cart.Select(static kvp => new CommerceCartItemDto(kvp.Key, kvp.Value)),
        ];
        var res = await QueryDispatcher.QueryAsync<
            PreviewCommerceOrderQuoteQuery,
            CommerceOrderQuoteResponse
        >(
            new PreviewCommerceOrderQuoteQuery(
                items,
                ExplicitMembershipTier: string.IsNullOrWhiteSpace(_memberTierInput)
                    ? null
                    : _memberTierInput,
                FulfillmentMethod: _fulfillmentMethod
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _quote = res.Value;
        _fulfillmentMethod = res.Value.FulfillmentMethod;
        _isError = false;
    }

    private async Task SubmitCheckoutAsync()
    {
        CommerceCartItemDto[] items =
        [
            .. _cart.Select(static kvp => new CommerceCartItemDto(kvp.Key, kvp.Value)),
        ];
        var res = await CommandDispatcher.SendAsync<
            SubmitCommerceOrderCommand,
            CommerceOrderResponse
        >(
            new SubmitCommerceOrderCommand(
                _buyerName,
                _buyerEmail,
                items,
                ExplicitMembershipTier: string.IsNullOrWhiteSpace(_memberTierInput)
                    ? null
                    : _memberTierInput,
                FulfillmentMethod: _fulfillmentMethod,
                ShippingAddressSummary: _fulfillmentMethod
                == CommerceFulfillmentMethod.FlatRateShipping
                    ? _shippingAddress
                    : null,
                OneTimeHostedPaymentToken: _paymentToken
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _completedOrder = res.Value;
        _isError = false;
        _statusMessage = $"Completed storefront order {res.Value.OrderNumber}.";
    }

    private async Task RequestDownloadLinkAsync(string orderNumber, Guid lineId)
    {
        var res = await QueryDispatcher.QueryAsync<
            GetOrderDigitalDownloadLinkQuery,
            OrderDigitalDownloadLinkResponse
        >(new GetOrderDigitalDownloadLinkQuery(orderNumber, lineId));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _downloadUrl = res.Value.DownloadUrl;
        _isError = false;
    }
}
