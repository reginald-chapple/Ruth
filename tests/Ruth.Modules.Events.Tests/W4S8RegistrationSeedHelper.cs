using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Events.Tests;

internal static class W4S8RegistrationSeedHelper
{
    public static async Task<(
        Guid AccountId,
        Guid GatewayId,
        Guid GroupTypeId,
        Guid GroupId,
        Guid Person1Id,
        Guid Person2Id
    )> SeedDependenciesAsync(ICommandDispatcher commands, IQueryDispatcher queries)
    {
        var accRes = await queries.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: false));
        Assert.True(accRes.IsSuccess);
        var accountId = accRes.Value[0].Id;

        var gwRes = await queries.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery(ActiveOnly: true));
        Assert.True(gwRes.IsSuccess);
        var gatewayId = gwRes.Value[0].Id;

        var gtRes = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand("Event Registration Cohort")
        );
        Assert.True(gtRes.IsSuccess);

        var grpRes = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gtRes.Value.Id, "Retreat Cohort A")
        );
        Assert.True(grpRes.IsSuccess);

        var p1 = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Elena", "Vance", Email: "elena@example.org")
        );
        Assert.True(p1.IsSuccess);

        var p2 = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand("Marcus", "Cole", Email: "marcus@example.org")
        );
        Assert.True(p2.IsSuccess);

        return (accountId, gatewayId, gtRes.Value.Id, grpRes.Value.Id, p1.Value.Id, p2.Value.Id);
    }
}
