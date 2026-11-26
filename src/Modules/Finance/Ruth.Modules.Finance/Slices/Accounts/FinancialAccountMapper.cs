using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Domain.Accounts;

namespace Ruth.Modules.Finance.Slices.Accounts;

internal static class FinancialAccountMapper
{
    public static FinancialAccountResponse ToResponse(FinancialAccount entity)
    {
        return new FinancialAccountResponse(
            entity.Id.Value,
            entity.ParentAccountId?.Value,
            entity.Name,
            entity.PublicName,
            entity.Code,
            entity.Description,
            entity.AccountKind,
            entity.IsTaxDeductible,
            entity.DefaultCurrencyCode,
            entity.BranchId,
            entity.GlCode,
            entity.CostCenterCode,
            entity.StartDate,
            entity.EndDate,
            entity.DisplayOrder,
            entity.IsActive,
            entity.IsPublic,
            new Dictionary<string, string?>(
                entity.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }
}
