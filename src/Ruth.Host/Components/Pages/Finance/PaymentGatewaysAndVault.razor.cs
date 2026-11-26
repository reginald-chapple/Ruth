using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Admin workspace for configuring payment gateways and vaulting hosted payment tokens.</summary>
public sealed partial class PaymentGatewaysAndVault : ComponentBase
{
    private List<RegisteredPaymentGatewayProviderResponse> _providers = [];
    private List<FinancialGatewayResponse> _gateways = [];
    private List<SavedPaymentMethodResponse> _savedMethods = [];

    private string _gatewayName = string.Empty;
    private string _providerKey = "test";
    private bool _isTestMode = true;

    private Guid _selectedGatewayId;
    private string _personIdInput = Guid.CreateVersion7().ToString("D");
    private string _last4Hint = "4242";
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var provRes = await QueryDispatcher.QueryAsync<
            GetRegisteredPaymentGatewayProvidersQuery,
            IReadOnlyList<RegisteredPaymentGatewayProviderResponse>
        >(new GetRegisteredPaymentGatewayProvidersQuery());
        if (provRes.IsSuccess)
        {
            _providers = [.. provRes.Value];
        }

        var gwRes = await QueryDispatcher.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery());
        if (gwRes.IsSuccess)
        {
            _gateways = [.. gwRes.Value];
            if (_selectedGatewayId == Guid.Empty && _gateways.Count > 0)
            {
                _selectedGatewayId = _gateways[0].Id;
            }
        }

        var methodsRes = await QueryDispatcher.QueryAsync<
            GetSavedPaymentMethodsQuery,
            IReadOnlyList<SavedPaymentMethodResponse>
        >(new GetSavedPaymentMethodsQuery());
        if (methodsRes.IsSuccess)
        {
            _savedMethods = [.. methodsRes.Value];
        }
    }

    private async Task CreateGatewayAsync()
    {
        var result = await CommandDispatcher.SendAsync<
            CreateFinancialGatewayCommand,
            FinancialGatewayResponse
        >(
            new CreateFinancialGatewayCommand(
                new FinancialGatewaySpec(_gatewayName, _providerKey, IsTestMode: _isTestMode)
            )
        );
        if (result.IsFailure)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _gatewayName = string.Empty;
        _isError = false;
        _statusMessage = $"Created gateway {result.Value.Name}.";
        await ReloadAsync();
    }

    private async Task TokenizeAndVaultAsync()
    {
        if (!Guid.TryParse(_personIdInput, out var personId) || personId == Guid.Empty)
        {
            _isError = true;
            _statusMessage = "Enter a valid Person GUID.";
            return;
        }

        await using var module = await JsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./ruth-payment-tokenizer.js"
        );
        var tokenPayload = await module.InvokeAsync<HostedTokenPayload>(
            "tokenizeHostedPayment",
            new { methodKind = "CreditCard", last4Hint = _last4Hint }
        );

        var vaulted = await CommandDispatcher.SendAsync<
            VaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(
            new VaultSavedPaymentMethodCommand(
                personId,
                _selectedGatewayId,
                tokenPayload.HostedPaymentToken,
                PaymentMethodKind.CreditCard,
                MaskedLast4Hint: tokenPayload.MaskedLast4,
                CardBrandHint: tokenPayload.CardBrand,
                ExpiryMonthHint: tokenPayload.ExpiryMonth,
                ExpiryYearHint: tokenPayload.ExpiryYear
            )
        );
        if (vaulted.IsFailure)
        {
            _isError = true;
            _statusMessage = vaulted.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Vaulted {vaulted.Value.DisplayName}.";
        await ReloadAsync();
    }

    private sealed record HostedTokenPayload(
        bool Ok,
        string HostedPaymentToken,
        string MaskedLast4,
        string CardBrand,
        int? ExpiryMonth,
        int? ExpiryYear
    );
}
