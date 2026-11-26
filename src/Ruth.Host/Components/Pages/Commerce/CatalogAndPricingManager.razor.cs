using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Commerce;

/// <summary>Staff catalog manager for products, variants, punch pass packs, IRS quid-pro-quo splits, and member pricing rules.</summary>
public sealed partial class CatalogAndPricingManager : ComponentBase
{
    private static readonly ProductKind[] _productKinds =
    [
        ProductKind.PhysicalPickupOrFlatRate,
        ProductKind.PunchPassPack,
        ProductKind.DigitalDownload,
    ];

    private IReadOnlyList<FinancialAccountResponse> _nonDeductibleAccounts = [];
    private IReadOnlyList<FinancialAccountResponse> _deductibleAccounts = [];
    private IReadOnlyList<PunchPassTypeResponse> _punchPassTypes = [];
    private IReadOnlyList<ProductResponse> _products = [];
    private IReadOnlyList<MemberPricingRuleResponse> _rules = [];

    private string _passName = "10-Visit Facility Pass";
    private string _passCode = "VISIT-10";
    private string _passCreditsInput = "10";
    private string _passValidityDaysInput = "180";

    private string _productName = "Benefactor Supporter Bundle";
    private string _productSlug = "benefactor-supporter-bundle";
    private ProductKind _selectedKind = ProductKind.PhysicalPickupOrFlatRate;
    private Guid _revenueAccountId;
    private Guid _charitableAccountId;
    private Guid _linkedPunchPassTypeId;
    private string _sku = "SUPP-BNDL-01";
    private string _variantName = "Standard Edition";
    private string _unitPriceInput = "100.00";
    private string _fmvInput = "35.00";
    private string _taxRateInput = "7.50";
    private string _stockInput = "25";

    private string _ruleName = "Gold Member 20% Off";
    private string _ruleTier = "Gold";
    private string _rulePercentInput = "20";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAllAsync();
    }

    private async Task ReloadAllAsync()
    {
        var accRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true));
        if (accRes.IsSuccess)
        {
            _nonDeductibleAccounts = [.. accRes.Value.Where(static a => !a.IsTaxDeductible)];
            _deductibleAccounts = [.. accRes.Value.Where(static a => a.IsTaxDeductible)];
            if (_revenueAccountId == Guid.Empty && _nonDeductibleAccounts.Count > 0)
            {
                _revenueAccountId = _nonDeductibleAccounts[0].Id;
            }

            if (_charitableAccountId == Guid.Empty && _deductibleAccounts.Count > 0)
            {
                _charitableAccountId = _deductibleAccounts[0].Id;
            }
        }

        var passRes = await QueryDispatcher.QueryAsync<
            GetPunchPassTypesQuery,
            IReadOnlyList<PunchPassTypeResponse>
        >(new GetPunchPassTypesQuery());
        _punchPassTypes = passRes.IsSuccess ? passRes.Value : [];
        if (_linkedPunchPassTypeId == Guid.Empty && _punchPassTypes.Count > 0)
        {
            _linkedPunchPassTypeId = _punchPassTypes[0].Id;
        }

        var prodRes = await QueryDispatcher.QueryAsync<
            GetProductsQuery,
            IReadOnlyList<ProductResponse>
        >(new GetProductsQuery());
        _products = prodRes.IsSuccess ? prodRes.Value : [];

        var rulesRes = await QueryDispatcher.QueryAsync<
            GetMemberPricingRulesQuery,
            IReadOnlyList<MemberPricingRuleResponse>
        >(new GetMemberPricingRulesQuery());
        _rules = rulesRes.IsSuccess ? rulesRes.Value : [];
    }

    private async Task CreatePunchPassTypeAsync()
    {
        if (
            !int.TryParse(
                _passCreditsInput,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var credits
            )
        )
        {
            SetError("Enter a valid positive credit count.");
            return;
        }

        int? validity = int.TryParse(
            _passValidityDaysInput,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var days
        )
            ? days
            : null;

        var res = await CommandDispatcher.SendAsync<
            CreatePunchPassTypeCommand,
            PunchPassTypeResponse
        >(
            new CreatePunchPassTypeCommand(
                new PunchPassTypeSpec(
                    _passName,
                    _passCode,
                    credits,
                    validity,
                    AllowHouseholdSharing: true
                )
            )
        );
        if (res.IsFailure)
        {
            SetError(res.PrimaryError.Description);
            return;
        }

        await ReloadAllAsync();
        SetSuccess($"Created punch pass type {res.Value.Code}.");
    }

    private async Task CreateProductAsync()
    {
        if (
            !CommerceCatalogSpecFactory.TryBuildProductSpec(
                _productName,
                _productSlug,
                _selectedKind,
                _revenueAccountId,
                _charitableAccountId,
                _linkedPunchPassTypeId,
                _sku,
                _variantName,
                _unitPriceInput,
                _fmvInput,
                _taxRateInput,
                _stockInput,
                out var spec
            )
        )
        {
            SetError("Enter valid numeric price, FMV, tax rate, and stock values.");
            return;
        }

        var res = await CommandDispatcher.SendAsync<CreateProductCommand, ProductResponse>(
            new CreateProductCommand(spec!)
        );
        if (res.IsFailure)
        {
            SetError(res.PrimaryError.Description);
            return;
        }

        await ReloadAllAsync();
        SetSuccess($"Created catalog product {res.Value.Name} ({res.Value.Slug}).");
    }

    private async Task CreateMemberRuleAsync()
    {
        if (
            !decimal.TryParse(
                _rulePercentInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var pct
            )
        )
        {
            SetError("Enter a valid discount percentage.");
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateMemberPricingRuleCommand,
            MemberPricingRuleResponse
        >(
            new CreateMemberPricingRuleCommand(
                new MemberPricingRuleSpec(
                    _ruleName,
                    MemberDiscountKind.Percentage,
                    pct,
                    RequiredMembershipTier: _ruleTier
                )
            )
        );
        if (res.IsFailure)
        {
            SetError(res.PrimaryError.Description);
            return;
        }

        await ReloadAllAsync();
        SetSuccess($"Created member pricing rule {res.Value.Name}.");
    }

    private void SetError(string message)
    {
        _isError = true;
        _statusMessage = message;
    }

    private void SetSuccess(string message)
    {
        _isError = false;
        _statusMessage = message;
    }
}
