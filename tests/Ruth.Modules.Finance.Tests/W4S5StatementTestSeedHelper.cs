using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Tests;

internal static class W4S5StatementTestSeedHelper
{
    public static async Task<(
        Guid AdultA,
        Guid HouseholdId,
        Guid SponsorOrgId
    )> SeedHouseholdAndSponsorLedgerActivityAsync(
        ICommandDispatcher commands,
        Guid deductibleAccountId,
        Guid duesAccountId
    )
    {
        var p1 = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Elena", "Vance", Email: "elena@ruth.test")
            )
        ).Value;
        var p2 = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Marcus", "Vance", Email: "marcus@ruth.test")
            )
        ).Value;

        var hh = (
            await commands.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
                new CreateHouseholdCommand(
                    "Vance Household",
                    CombineContributionsByDefault: true,
                    Members:
                    [
                        new HouseholdMemberInputDto(
                            p1.Id,
                            HouseholdRole.Adult,
                            HouseholdOrder: 0,
                            IsPrimaryContact: true
                        ),
                        new HouseholdMemberInputDto(
                            p2.Id,
                            HouseholdRole.Adult,
                            HouseholdOrder: 1,
                            IsPrimaryContact: false
                        ),
                    ]
                )
            )
        ).Value;

        var sponsor = (
            await commands.SendAsync<CreateOrganizationCommand, OrganizationDetailResponse>(
                new CreateOrganizationCommand(
                    "Apex Civic Foundation",
                    Email: "grants@apex.test",
                    SponsorTier: "Platinum"
                )
            )
        ).Value;

        _ = await commands.SendAsync<CreateFinancialPledgeCommand, FinancialPledgeResponse>(
            new CreateFinancialPledgeCommand(
                p1.Id,
                deductibleAccountId,
                1000.00m,
                "USD",
                PledgeFrequency.Annually,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31)
            )
        );

        await RecordSeedTransactionsAsync(
            commands,
            p1.Id,
            p2.Id,
            hh.Id,
            sponsor.OrganizationPersonId,
            deductibleAccountId,
            duesAccountId
        );

        return (p1.Id, hh.Id, sponsor.OrganizationPersonId);
    }

    private static async Task RecordSeedTransactionsAsync(
        ICommandDispatcher commands,
        Guid adultA,
        Guid adultB,
        Guid householdId,
        Guid sponsorOrgId,
        Guid deductibleAccountId,
        Guid duesAccountId
    )
    {
        var txDate = new DateTimeOffset(2026, 5, 10, 15, 0, 0, TimeSpan.Zero);
        _ = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                adultA,
                "USD",
                FinancialTransactionSourceType.Website,
                FinancialTenderType.CreditCard,
                [
                    new FinancialTransactionSplitSpec(
                        deductibleAccountId,
                        300.00m,
                        Summary: "Annual Fund"
                    ),
                    new FinancialTransactionSplitSpec(
                        duesAccountId,
                        75.00m,
                        Summary: "Member Dues"
                    ),
                ],
                TransactionDateTimeUtc: txDate,
                HouseholdId: householdId
            )
        );

        _ = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                adultB,
                "USD",
                FinancialTransactionSourceType.Website,
                FinancialTenderType.Ach,
                [new FinancialTransactionSplitSpec(deductibleAccountId, 200.00m)],
                TransactionDateTimeUtc: txDate.AddDays(5),
                HouseholdId: householdId
            )
        );

        _ = await commands.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                sponsorOrgId,
                "USD",
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Wire,
                [new FinancialTransactionSplitSpec(deductibleAccountId, 2500.00m)],
                TransactionDateTimeUtc: txDate.AddDays(10)
            )
        );
    }
}
