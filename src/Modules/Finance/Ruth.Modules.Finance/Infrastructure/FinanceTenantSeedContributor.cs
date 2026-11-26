using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Finance.Infrastructure;

internal sealed class FinanceTenantSeedContributor(FinanceDbContext dbContext)
    : ITenantSeedContributor
{
    public int Order => 60;

    public async Task SeedTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        await SeedDefaultFinancialAccountsAsync(dbContext, cancellationToken);
        await SeedDefaultTestGatewayAsync(dbContext, cancellationToken);
        await SeedDefaultCadenceAlertTypesAsync(dbContext, cancellationToken);
        await SeedDefaultStatementTemplatesAsync(dbContext, cancellationToken);
    }

    private static async Task SeedDefaultFinancialAccountsAsync(
        FinanceDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.FinancialAccounts.AnyAsync(cancellationToken))
        {
            return;
        }

        FinancialAccountSpec[] defaults =
        [
            new(
                "General Charitable Fund",
                "General Fund",
                "GEN-FUND",
                FinancialAccountKind.CharitableFund,
                IsTaxDeductible: true,
                DisplayOrder: 1
            ),
            new(
                "Standard Membership Dues",
                "Membership Dues",
                "MEM-DUES",
                FinancialAccountKind.MembershipDues,
                IsTaxDeductible: false,
                DisplayOrder: 2
            ),
            new(
                "Program & Service Fees",
                "Program Fees",
                "PROG-FEES",
                FinancialAccountKind.ProgramFee,
                IsTaxDeductible: false,
                DisplayOrder: 3
            ),
        ];

        foreach (var spec in defaults)
        {
            var created = FinancialAccount.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.FinancialAccounts.Add(created.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDefaultTestGatewayAsync(
        FinanceDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.FinancialGateways.AnyAsync(cancellationToken))
        {
            return;
        }

        var created = FinancialGateway.Create(
            new FinancialGatewaySpec(
                Name: "Default Sandbox Gateway",
                ProviderKey: TestPaymentGatewayProvider.CanonicalProviderKey,
                Description: "Built-in deterministic test gateway for card and ACH tokenization.",
                SupportsCards: true,
                SupportsAch: true,
                SupportsRecurring: true,
                IsTestMode: true,
                IsActive: true
            )
        );
        if (created.IsSuccess)
        {
            _ = dbContext.FinancialGateways.Add(created.Value);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDefaultCadenceAlertTypesAsync(
        FinanceDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.CadenceAlertTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        TransactionCadenceAlertTypeSpec[] defaults =
        [
            new(
                "Constituent Cadence Lapse / Churn Risk",
                CadenceAlertKind.LapseOrChurnRisk,
                MinHistoricalSamples: 3,
                MultiplierThreshold: 2.0m,
                StandardDeviationSensitivity: 1.5m,
                MinimumAmountThreshold: 25m,
                CooldownDays: 30
            ),
            new(
                "Unusual Transaction Amount Spike",
                CadenceAlertKind.UnusualAmountSpike,
                MinHistoricalSamples: 3,
                MultiplierThreshold: 3.0m,
                StandardDeviationSensitivity: 2.0m,
                MinimumAmountThreshold: 250m,
                CooldownDays: 14
            ),
            new(
                "Past-Due Recurring Subscription Alert",
                CadenceAlertKind.SubscriptionPastDue,
                MinHistoricalSamples: 2,
                MultiplierThreshold: 1.5m,
                StandardDeviationSensitivity: 0m,
                MinimumAmountThreshold: 0m,
                CooldownDays: 14
            ),
        ];

        foreach (var spec in defaults)
        {
            var created = TransactionCadenceAlertType.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.CadenceAlertTypes.Add(created.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDefaultStatementTemplatesAsync(
        FinanceDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.StatementTemplates.AnyAsync(cancellationToken))
        {
            return;
        }

        FinancialStatementTemplateSpec[] defaults =
        [
            new(
                Name: "Annual Tax-Deductible Contribution Receipt",
                StatementKind: FinancialStatementKind.TaxDeductibleReceipt,
                OrganizationDisplayName: "Organization",
                HeaderMergeTemplate: "<p>Thank you, {{ RecipientName }}, for your charitable support during {{ PeriodStart }} through {{ PeriodEnd }}.</p>",
                FooterMergeTemplate: "<p>No goods or services were provided in exchange for these charitable contributions ({{ TaxDeductibleTotal }} {{ CurrencyCode }}).</p>",
                IsDefault: true
            ),
            new(
                Name: "Account Dues, Fees & Invoice Statement",
                StatementKind: FinancialStatementKind.NonDeductibleStatement,
                OrganizationDisplayName: "Organization",
                HeaderMergeTemplate: "<p>Account statement for {{ RecipientName }} covering {{ PeriodStart }} through {{ PeriodEnd }}.</p>",
                FooterMergeTemplate: "<p>Payments toward membership dues, fees, and invoices are not deductible as charitable contributions.</p>",
                IsDefault: true
            ),
        ];

        foreach (var spec in defaults)
        {
            var created = FinancialStatementTemplate.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.StatementTemplates.Add(created.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
