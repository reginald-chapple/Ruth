using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class ProductCatalogValidator(
    CommerceDbContext dbContext,
    IQueryDispatcher queryDispatcher
)
{
    public async Task<Result> ValidateAccountsAndUniquenessAsync(
        ProductSpec spec,
        ProductId? currentProductId,
        CancellationToken cancellationToken
    )
    {
        var revRes = await queryDispatcher.QueryAsync<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse
        >(new GetFinancialAccountByIdQuery(spec.RevenueAccountId), cancellationToken);
        if (revRes.IsFailure || !revRes.Value.IsActive)
        {
            return CommerceErrors.ProductInvalid;
        }

        if (spec.IsQuidProQuo)
        {
            var qpqRes = await ValidateQuidProQuoAccountsAsync(
                revRes.Value,
                spec.QuidProQuoCharitableAccountId,
                cancellationToken
            );
            if (qpqRes.IsFailure)
            {
                return qpqRes;
            }
        }

        if (spec.Kind == ProductKind.PunchPassPack)
        {
            var ppRes = await ValidatePunchPassVariantsAsync(spec.Variants, cancellationToken);
            if (ppRes.IsFailure)
            {
                return ppRes;
            }
        }

        return await CheckSlugAndSkuUniquenessAsync(spec, currentProductId, cancellationToken);
    }

    private async Task<Result> ValidateQuidProQuoAccountsAsync(
        FinancialAccountResponse revenueAccount,
        Guid? charitableAccountId,
        CancellationToken cancellationToken
    )
    {
        if (revenueAccount.IsTaxDeductible || charitableAccountId is null)
        {
            return CommerceErrors.QuidProQuoAccountInvalid;
        }

        var charRes = await queryDispatcher.QueryAsync<
            GetFinancialAccountByIdQuery,
            FinancialAccountResponse
        >(new GetFinancialAccountByIdQuery(charitableAccountId.Value), cancellationToken);

        return charRes.IsFailure || !charRes.Value.IsActive || !charRes.Value.IsTaxDeductible
            ? CommerceErrors.QuidProQuoAccountInvalid
            : Result.Success();
    }

    private async Task<Result> ValidatePunchPassVariantsAsync(
        IReadOnlyList<ProductVariantSpec> variants,
        CancellationToken cancellationToken
    )
    {
        var ids = variants
            .Where(static v => v.PunchPassTypeId.HasValue)
            .Select(static v => PunchPassTypeId.From(v.PunchPassTypeId!.Value))
            .Distinct()
            .ToArray();

        var existingCount = await dbContext.PunchPassTypes.CountAsync(
            p => ids.Contains(p.Id) && p.IsActive,
            cancellationToken
        );
        return existingCount == ids.Length ? Result.Success() : CommerceErrors.ProductInvalid;
    }

    private async Task<Result> CheckSlugAndSkuUniquenessAsync(
        ProductSpec spec,
        ProductId? currentProductId,
        CancellationToken cancellationToken
    )
    {
        var normalizedSlug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var skus = spec
            .Variants.Select(static v => v.Sku?.Trim().ToUpperInvariant() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var slugConflict = await dbContext.Products.AnyAsync(
            p =>
                p.Slug == normalizedSlug
                && (!currentProductId.HasValue || p.Id != currentProductId.Value),
            cancellationToken
        );
        if (slugConflict)
        {
            return CommerceErrors.ProductSlugOrSkuConflict;
        }

        var skuConflict = await dbContext.ProductVariants.AnyAsync(
            v =>
                skus.Contains(v.Sku)
                && (!currentProductId.HasValue || v.ProductId != currentProductId.Value),
            cancellationToken
        );
        return skuConflict ? CommerceErrors.ProductSlugOrSkuConflict : Result.Success();
    }
}
