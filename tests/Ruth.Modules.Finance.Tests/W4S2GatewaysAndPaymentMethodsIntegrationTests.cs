using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Finance.Tests;

[Collection(FinancePostgresFixtures.Name)]
public sealed class W4S2GatewaysAndPaymentMethodsIntegrationTests(
    FinancePostgresContainerFixture postgres
)
{
    [Fact]
    public async Task Gateways_SavedPaymentMethods_AndBankAccountIdentifiers_PersistOpaqueTokensOnly()
    {
        var db = await postgres.CreateIsolatedDatabaseAsync();
        await using var root = await FinanceTestHostFactory.BuildAndMigrateAsync(db);
        await using var scope = root.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var providers = await queries.QueryAsync<
            GetRegisteredPaymentGatewayProvidersQuery,
            IReadOnlyList<RegisteredPaymentGatewayProviderResponse>
        >(new GetRegisteredPaymentGatewayProvidersQuery());
        Assert.True(providers.IsSuccess);
        Assert.Contains(providers.Value, static p =>
            string.Equals(p.ProviderKey, "test", StringComparison.Ordinal)
        );
        Assert.Contains(providers.Value, static p =>
            string.Equals(p.ProviderKey, "nmi", StringComparison.Ordinal)
        );
        Assert.Contains(providers.Value, static p =>
            string.Equals(p.ProviderKey, "stripe", StringComparison.Ordinal)
        );

        var gateways = await queries.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery(ActiveOnly: true));
        Assert.True(gateways.IsSuccess);
        var testGatewayId = gateways.Value[0].Id;

        var personId = Guid.CreateVersion7();
        var card1 = await commands.SendAsync<
            VaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(
            new VaultSavedPaymentMethodCommand(
                personId,
                testGatewayId,
                "tok_visa_1",
                PaymentMethodKind.CreditCard,
                DisplayName: "Personal Visa",
                MaskedLast4Hint: "4242",
                CardBrandHint: "Visa",
                ExpiryMonthHint: 10,
                ExpiryYearHint: 2029,
                SetAsDefault: true
            )
        );
        Assert.True(card1.IsSuccess);
        Assert.True(card1.Value.IsDefault);

        var ach2 = await commands.SendAsync<
            VaultSavedPaymentMethodCommand,
            SavedPaymentMethodResponse
        >(
            new VaultSavedPaymentMethodCommand(
                personId,
                testGatewayId,
                "tok_ach_2",
                PaymentMethodKind.AchBankAccount,
                DisplayName: "Operating Checking",
                MaskedLast4Hint: "8899",
                SetAsDefault: true
            )
        );
        Assert.True(ach2.IsSuccess);

        var savedList = await queries.QueryAsync<
            GetSavedPaymentMethodsQuery,
            IReadOnlyList<SavedPaymentMethodResponse>
        >(new GetSavedPaymentMethodsQuery(personId));
        Assert.Equal(2, savedList.Value.Count);
        _ = Assert.Single(savedList.Value, static m => m.IsDefault);

        await VerifyDefaultToggleDeactivateAndBankLookupAsync(
            commands,
            queries,
            personId,
            card1.Value.Id,
            ach2.Value.Id
        );
    }

    private static async Task VerifyDefaultToggleDeactivateAndBankLookupAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId,
        Guid cardId,
        Guid achId
    )
    {
        Assert.True(
            (
                await commands.SendAsync<
                    SetDefaultSavedPaymentMethodCommand,
                    SavedPaymentMethodResponse
                >(new SetDefaultSavedPaymentMethodCommand(cardId))
            ).IsSuccess
        );
        Assert.True(
            (
                await commands.SendAsync<
                    DeactivateSavedPaymentMethodCommand,
                    SavedPaymentMethodResponse
                >(new DeactivateSavedPaymentMethodCommand(achId))
            ).IsSuccess
        );

        var bankReg = await commands.SendAsync<
            RegisterBankAccountIdentifierCommand,
            BankAccountIdentifierResponse
        >(
            new RegisterBankAccountIdentifierCommand(
                personId,
                "021000021",
                "123456789",
                "Community Bank"
            )
        );
        Assert.True(bankReg.IsSuccess);

        var resolved = await queries.QueryAsync<
            ResolvePersonByBankAccountQuery,
            BankAccountIdentifierResponse
        >(new ResolvePersonByBankAccountQuery("0210-0002-1", "123456789"));
        Assert.True(resolved.IsSuccess);
        Assert.Equal(personId, resolved.Value.PersonId);
    }
}
