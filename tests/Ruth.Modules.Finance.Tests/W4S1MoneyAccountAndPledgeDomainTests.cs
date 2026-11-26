using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Pledges;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S1MoneyAccountAndPledgeDomainTests
{
    [Fact]
    public void Money_EnforcesIso4217Scales_CurrencyMatching_AndPositiveBounds()
    {
        var usd = Money.Create(125.50m, "usd");
        Assert.True(usd.IsSuccess);
        Assert.Equal("USD", usd.Value.CurrencyCode);
        Assert.Equal("125.50 USD", usd.Value.ToString());

        Assert.True(Money.Create(1000m, "JPY").IsSuccess);
        Assert.True(Money.Create(1000.5m, "JPY").IsFailure);
        Assert.True(Money.Create(12.345m, "KWD").IsSuccess);
        Assert.True(Money.Create(12.3456m, "KWD").IsFailure);
        Assert.True(Money.Create(10m, "INVALID").IsFailure);

        Assert.True(Money.CreatePositive(0m, "USD").IsFailure);
        Assert.True(Money.CreatePositive(10m, "USD").IsSuccess);
        Assert.True(Money.CreateNonNegative(-0.01m, "USD").IsFailure);
        Assert.True(Money.Zero("EUR").IsSuccess);

        var eur = Money.Create(25.00m, "EUR").Value;
        Assert.True(usd.Value.Add(eur).IsFailure);
        Assert.True(usd.Value.Subtract(eur).IsFailure);

        var sum = usd.Value.Add(Money.Create(24.50m, "USD").Value);
        Assert.True(sum.IsSuccess);
        Assert.Equal(150.00m, sum.Value.Amount);

        var diff = sum.Value.Subtract(Money.Create(50.00m, "USD").Value);
        Assert.True(diff.IsSuccess);
        Assert.Equal(100.00m, diff.Value.Amount);
    }

    [Fact]
    public void Money_Allocate_DistributesMinorUnitsWithoutPennyLoss()
    {
        var total = Money.Create(100.00m, "USD").Value;
        var allocated = total.Allocate([1m, 1m, 1m]);
        Assert.True(allocated.IsSuccess);
        Assert.Equal(3, allocated.Value.Count);
        Assert.Equal(33.34m, allocated.Value[0].Amount);
        Assert.Equal(33.33m, allocated.Value[1].Amount);
        Assert.Equal(33.33m, allocated.Value[2].Amount);
        Assert.Equal(100.00m, allocated.Value.Sum(static m => m.Amount));

        var jpyAllocated = Money.Create(10m, "JPY").Value.Allocate([0m, 1m, 2m]);
        Assert.True(jpyAllocated.IsSuccess);
        Assert.Equal(0m, jpyAllocated.Value[0].Amount);
        Assert.Equal(10m, jpyAllocated.Value.Sum(static m => m.Amount));

        var kwdAllocated = Money.Create(1.000m, "KWD").Value.Allocate([1m, 1m, 1m]);
        Assert.True(kwdAllocated.IsSuccess);
        Assert.Equal(1.000m, kwdAllocated.Value.Sum(static m => m.Amount));

        Assert.True(total.Allocate([]).IsFailure);
        Assert.True(total.Allocate([0m, 0m]).IsFailure);
        Assert.True(total.Allocate([1m, -1m]).IsFailure);
    }

    [Fact]
    public void FinancialAccount_EnforcesCodeDateHierarchyAndOpenWindowInvariants()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 12, 31);
        var created = FinancialAccount.Create(
            new FinancialAccountSpec(
                "Annual Scholarship Fund",
                "Scholarship Fund",
                "SCHOLAR-2026",
                FinancialAccountKind.CharitableFund,
                IsTaxDeductible: true,
                DefaultCurrencyCode: "USD",
                GlCode: "4100",
                CostCenterCode: "CC-10",
                Description: "Supports community scholarships",
                StartDate: start,
                EndDate: end,
                Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["fund_restriction"] = "TemporarilyRestricted",
                }
            )
        );
        Assert.True(created.IsSuccess);
        var account = created.Value;
        Assert.True(account.IsOpenOn(new DateOnly(2026, 6, 15)));
        Assert.False(account.IsOpenOn(new DateOnly(2025, 12, 31)));
        Assert.False(account.IsOpenOn(new DateOnly(2027, 1, 1)));
        Assert.Equal("TemporarilyRestricted", account.AttributeValues["fund_restriction"]);

        account.SetAttributeValue("campaign_year", "2026");
        account.RemoveAttributeValue("campaign_year");
        Assert.False(account.AttributeValues.ContainsKey("campaign_year"));

        Assert.True(
            FinancialAccount
                .Create(
                    new FinancialAccountSpec(
                        "",
                        PublicName: null,
                        "VALID-CODE",
                        FinancialAccountKind.ProgramFee,
                        IsTaxDeductible: false
                    )
                )
                .IsFailure
        );
        Assert.True(
            FinancialAccount
                .Create(
                    new FinancialAccountSpec(
                        "Valid",
                        PublicName: null,
                        "bad code!",
                        FinancialAccountKind.ProgramFee,
                        IsTaxDeductible: false
                    )
                )
                .IsFailure
        );
        Assert.True(
            FinancialAccount
                .Create(
                    new FinancialAccountSpec(
                        "Valid",
                        PublicName: null,
                        "DATE-ERR",
                        FinancialAccountKind.ProgramFee,
                        IsTaxDeductible: false,
                        StartDate: end,
                        EndDate: start
                    )
                )
                .IsFailure
        );
        Assert.True(
            account
                .Update(
                    new FinancialAccountSpec(
                        "Self Cycle",
                        PublicName: null,
                        "SCHOLAR-2026",
                        FinancialAccountKind.CharitableFund,
                        IsTaxDeductible: true,
                        ParentAccountId: account.Id.Value
                    )
                )
                .IsFailure
        );
    }

    [Theory]
    [InlineData(PledgeFrequency.OneTime, 1)]
    [InlineData(PledgeFrequency.Weekly, 53)]
    [InlineData(PledgeFrequency.BiWeekly, 27)]
    [InlineData(PledgeFrequency.SemiMonthly, 23)]
    [InlineData(PledgeFrequency.Monthly, 12)]
    [InlineData(PledgeFrequency.Quarterly, 4)]
    [InlineData(PledgeFrequency.Annually, 1)]
    public void FinancialPledge_CalculatesInstallmentCountsAcrossFrequencies(
        PledgeFrequency frequency,
        int expectedInstallments
    )
    {
        var pledge = FinancialPledge
            .Create(
                Guid.CreateVersion7(),
                FinancialAccountId.Create(),
                1200.00m,
                "USD",
                frequency,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31)
            )
            .Value;

        Assert.Equal(expectedInstallments, pledge.CalculateInstallmentCount());
    }

    [Fact]
    public void FinancialPledge_SupportsExpectedByDate_Revise_Complete_Cancel_AndPersonMerge()
    {
        var originalPerson = Guid.CreateVersion7();
        var survivingPerson = Guid.CreateVersion7();
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 1, 10);

        Assert.True(
            FinancialPledge
                .Create(
                    Guid.Empty,
                    FinancialAccountId.Create(),
                    100m,
                    "USD",
                    PledgeFrequency.Monthly,
                    start,
                    end
                )
                .IsFailure
        );
        Assert.True(
            FinancialPledge
                .Create(originalPerson, default, 100m, "USD", PledgeFrequency.Monthly, start, end)
                .IsFailure
        );

        var pledge = FinancialPledge
            .Create(
                originalPerson,
                FinancialAccountId.Create(),
                1000.00m,
                "USD",
                PledgeFrequency.Monthly,
                start,
                end,
                notes: "Initial commitment"
            )
            .Value;

        Assert.Equal(0m, pledge.CalculateExpectedByDate(new DateOnly(2025, 12, 31)));
        Assert.Equal(500.00m, pledge.CalculateExpectedByDate(new DateOnly(2026, 1, 5)));
        Assert.Equal(1000.00m, pledge.CalculateExpectedByDate(new DateOnly(2026, 1, 10)));

        Assert.True(
            pledge.Revise(1200.00m, PledgeFrequency.Monthly, end, start, notes: null).IsFailure
        );
        Assert.True(
            pledge.Revise(1500.00m, PledgeFrequency.Quarterly, start, end, "Updated").IsSuccess
        );

        Assert.False(pledge.RepointPerson(Guid.CreateVersion7(), survivingPerson));
        Assert.True(pledge.RepointPerson(originalPerson, survivingPerson));
        Assert.Equal(survivingPerson, pledge.PersonId);

        Assert.True(pledge.Cancel("Donor relocated").IsSuccess);
        Assert.Equal(0m, pledge.CalculateExpectedByDate(new DateOnly(2026, 1, 5)));
        Assert.True(pledge.Cancel("Duplicate cancel").IsFailure);
        Assert.True(
            pledge.Revise(500m, PledgeFrequency.Monthly, start, end, notes: null).IsFailure
        );
    }
}
