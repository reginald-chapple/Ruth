using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Domain.Recurring;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

public sealed class W4S4RecurringAndCadenceAnalyzerDomainTests
{
    [Fact]
    public void RecurringDateCalculator_PreservesMonthEndAnchorAndHandlesAllCadences()
    {
        var jan31 = new DateOnly(2026, 1, 31);
        var feb28 = RecurringDateCalculator.Advance(
            jan31,
            PledgeFrequency.Monthly,
            anchorDayOfMonth: 31
        );
        Assert.Equal(new DateOnly(2026, 2, 28), feb28);

        var mar31 = RecurringDateCalculator.Advance(
            feb28,
            PledgeFrequency.Monthly,
            anchorDayOfMonth: 31
        );
        Assert.Equal(new DateOnly(2026, 3, 31), mar31);

        var leapFeb = RecurringDateCalculator.Advance(
            new DateOnly(2028, 1, 31),
            PledgeFrequency.Monthly,
            anchorDayOfMonth: 31
        );
        Assert.Equal(new DateOnly(2028, 2, 29), leapFeb);

        Assert.Equal(
            new DateOnly(2026, 4, 8),
            RecurringDateCalculator.Advance(new DateOnly(2026, 4, 1), PledgeFrequency.Weekly, 1)
        );
        Assert.Equal(
            new DateOnly(2026, 4, 15),
            RecurringDateCalculator.Advance(new DateOnly(2026, 4, 1), PledgeFrequency.BiWeekly, 1)
        );
        Assert.Equal(
            new DateOnly(2026, 4, 15),
            RecurringDateCalculator.Advance(
                new DateOnly(2026, 4, 3),
                PledgeFrequency.SemiMonthly,
                3
            )
        );
        Assert.Equal(
            new DateOnly(2026, 5, 1),
            RecurringDateCalculator.Advance(
                new DateOnly(2026, 4, 15),
                PledgeFrequency.SemiMonthly,
                15
            )
        );
        Assert.Equal(
            new DateOnly(2026, 7, 15),
            RecurringDateCalculator.Advance(
                new DateOnly(2026, 4, 15),
                PledgeFrequency.Quarterly,
                15
            )
        );
        Assert.Equal(
            new DateOnly(2027, 4, 15),
            RecurringDateCalculator.Advance(new DateOnly(2026, 4, 15), PledgeFrequency.Annually, 15)
        );
    }

    [Fact]
    public void RecurringScheduleTransaction_EnforcesSettlementDunningPauseResumeCancelAndMerge()
    {
        var personId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();
        var gatewayId = FinancialGatewayId.Create();
        var methodId = SavedPaymentMethodId.Create();
        var accountId = FinancialAccountId.Create();
        var start = new DateOnly(2026, 1, 15);
        var now = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

        Assert.True(
            RecurringScheduleTransaction
                .Create(
                    personId,
                    gatewayId,
                    methodId,
                    RecurringSchedulePurpose.CharitableContribution,
                    PledgeFrequency.OneTime,
                    "USD",
                    start,
                    endDate: null,
                    [(accountId, 100m, "Invalid one-time")]
                )
                .IsFailure
        );

        var schedule = RecurringScheduleTransaction
            .Create(
                personId,
                gatewayId,
                methodId,
                RecurringSchedulePurpose.MembershipSubscription,
                PledgeFrequency.Monthly,
                "USD",
                start,
                new DateOnly(2026, 3, 20),
                [(accountId, 75.00m, "Monthly Membership")]
            )
            .Value;

        schedule.SetGatewaySubscriptionReference("sub_123");
        Assert.Equal("sub_123", schedule.GatewaySubscriptionReference);

        schedule.RecordFailedAttempt("Insufficient funds", now);
        schedule.RecordFailedAttempt("Card expired", now.AddDays(1));
        Assert.Equal(RecurringScheduleStatus.Active, schedule.Status);
        schedule.RecordFailedAttempt("Final retry declined", now.AddDays(2));
        Assert.Equal(RecurringScheduleStatus.PastDue, schedule.Status);

        schedule.AdvanceAfterSettlement(start, now.AddDays(3));
        Assert.Equal(RecurringScheduleStatus.Active, schedule.Status);
        Assert.Equal(0, schedule.ConsecutiveFailureCount);
        Assert.Equal(new DateOnly(2026, 2, 15), schedule.NextPaymentDate);

        Assert.True(schedule.Pause().IsSuccess);
        Assert.Equal(RecurringScheduleStatus.Paused, schedule.Status);
        Assert.True(schedule.Pause().IsFailure);
        Assert.True(schedule.Resume(new DateOnly(2026, 3, 15)).IsSuccess);
        Assert.Equal(RecurringScheduleStatus.Active, schedule.Status);

        Assert.True(
            schedule
                .UpdateSchedule(
                    SavedPaymentMethodId.Create(),
                    PledgeFrequency.Monthly,
                    new DateOnly(2026, 3, 15),
                    new DateOnly(2026, 3, 20),
                    [(accountId, 85.00m, "Upgraded Tier")]
                )
                .IsSuccess
        );
        Assert.Equal(85.00m, schedule.TotalAmount);

        schedule.AdvanceAfterSettlement(new DateOnly(2026, 3, 15), now.AddMonths(2));
        Assert.Equal(RecurringScheduleStatus.Completed, schedule.Status);

        Assert.True(schedule.RepointPerson(personId, survivingPersonId));
        Assert.Equal(survivingPersonId, schedule.PersonId);
        Assert.True(schedule.Cancel("Member closed account").IsSuccess);
        Assert.True(schedule.Cancel("Duplicate cancel").IsFailure);
    }

    [Fact]
    public void TransactionCadenceAnalyzer_DetectsLapseRisk_AmountSpike_AndPastDueSubscriptions()
    {
        Assert.True(
            TransactionCadenceAlertType
                .Create(
                    new TransactionCadenceAlertTypeSpec(
                        "",
                        CadenceAlertKind.LapseOrChurnRisk,
                        MinHistoricalSamples: 1,
                        MultiplierThreshold: 1.0m
                    )
                )
                .IsFailure
        );

        var lapseRule = TransactionCadenceAlertType
            .Create(
                new TransactionCadenceAlertTypeSpec(
                    "Monthly Donor Lapse",
                    CadenceAlertKind.LapseOrChurnRisk,
                    MinHistoricalSamples: 3,
                    MultiplierThreshold: 2.0m,
                    StandardDeviationSensitivity: 1.5m,
                    MinimumAmountThreshold: 50.00m
                )
            )
            .Value;

        HistoricalTransactionPoint[] steadyMonthly =
        [
            new(new DateOnly(2026, 1, 1), 100.00m),
            new(new DateOnly(2026, 1, 31), 100.00m),
            new(new DateOnly(2026, 3, 2), 100.00m),
            new(new DateOnly(2026, 4, 1), 100.00m),
        ];

        Assert.Null(
            TransactionCadenceAnalyzer.Evaluate(lapseRule, steadyMonthly, new DateOnly(2026, 4, 25))
        );

        var lapseFinding = Assert.NotNull(
            TransactionCadenceAnalyzer.Evaluate(lapseRule, steadyMonthly, new DateOnly(2026, 6, 15))
        );
        Assert.Equal(CadenceAlertKind.LapseOrChurnRisk, lapseFinding.Kind);
        Assert.Equal(75m, lapseFinding.ObservedValue);

        var spikeRule = TransactionCadenceAlertType
            .Create(
                new TransactionCadenceAlertTypeSpec(
                    "Major Spike",
                    CadenceAlertKind.UnusualAmountSpike,
                    MinHistoricalSamples: 3,
                    MultiplierThreshold: 3.0m,
                    StandardDeviationSensitivity: 2.0m,
                    MinimumAmountThreshold: 250.00m
                )
            )
            .Value;

        HistoricalTransactionPoint[] spikeSeries =
        [
            .. steadyMonthly,
            new(new DateOnly(2026, 5, 1), 1500.00m),
        ];
        var spikeFinding = Assert.NotNull(
            TransactionCadenceAnalyzer.Evaluate(spikeRule, spikeSeries, new DateOnly(2026, 5, 2))
        );
        Assert.Equal(CadenceAlertKind.UnusualAmountSpike, spikeFinding.Kind);
        Assert.Equal(1500.00m, spikeFinding.ObservedValue);

        var pastDueRule = TransactionCadenceAlertType
            .Create(
                new TransactionCadenceAlertTypeSpec(
                    "Subscription Past Due",
                    CadenceAlertKind.SubscriptionPastDue,
                    MinimumAmountThreshold: 25.00m
                )
            )
            .Value;
        Assert.Null(
            TransactionCadenceAnalyzer.Evaluate(
                pastDueRule,
                steadyMonthly,
                new DateOnly(2026, 5, 1),
                hasPastDueSubscription: false
            )
        );
        _ = Assert.NotNull(
            TransactionCadenceAnalyzer.Evaluate(
                pastDueRule,
                steadyMonthly,
                new DateOnly(2026, 5, 1),
                hasPastDueSubscription: true
            )
        );
    }
}
