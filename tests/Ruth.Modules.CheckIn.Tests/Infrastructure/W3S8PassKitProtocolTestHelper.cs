using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.CheckIn.Infrastructure.WalletPasses;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

internal static class W3S8PassKitProtocolTestHelper
{
    public static async Task VerifyPassKitDeviceLifecycleAndDynamicPushUpdatesAsync(
        ServiceProvider provider,
        IssuedWalletPassResponse issuedPass
    )
    {
        string rawAppleToken;
        await using (var tokenScope = provider.CreateAsyncScope())
        {
            var db = tokenScope.ServiceProvider.GetRequiredService<CheckInDbContext>();
            var passId = IssuedWalletPassId.From(issuedPass.Id);
            var passEntity = await db.IssuedWalletPasses.FindAsync(passId);
            Assert.NotNull(passEntity);
            var opts = new WalletPassSecurityOptions();
            rawAppleToken = WalletPassSecurityTokens.DeriveAppleAuthToken(
                opts.PassKitHmacSecret,
                passEntity.Id.Value,
                passEntity.AuthTokenSalt
            );
        }

        await using (var scope1 = provider.CreateAsyncScope())
        {
            var cmds = scope1.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var queries = scope1.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var badAuth = await cmds.SendAsync<RegisterPassKitDeviceCommand, bool>(
                new RegisterPassKitDeviceCommand(
                    "ios-device-01",
                    issuedPass.PassTypeIdentifier,
                    issuedPass.SerialNumber,
                    "apns-push-token-01",
                    "invalid-token-value-123456"
                )
            );
            Assert.True(badAuth.IsFailure);

            var firstReg = await cmds.SendAsync<RegisterPassKitDeviceCommand, bool>(
                new RegisterPassKitDeviceCommand(
                    "ios-device-01",
                    issuedPass.PassTypeIdentifier,
                    issuedPass.SerialNumber,
                    "apns-push-token-01",
                    rawAppleToken
                )
            );
            Assert.True(firstReg.Value);

            var dupReg = await cmds.SendAsync<RegisterPassKitDeviceCommand, bool>(
                new RegisterPassKitDeviceCommand(
                    "ios-device-01",
                    issuedPass.PassTypeIdentifier,
                    issuedPass.SerialNumber,
                    "apns-push-token-01",
                    rawAppleToken
                )
            );
            Assert.False(dupReg.Value);

            var initialSerials = (
                await queries.QueryAsync<
                    GetPassKitUpdatableSerialsQuery,
                    PassKitSerialNumbersResponse
                >(
                    new GetPassKitUpdatableSerialsQuery(
                        "ios-device-01",
                        issuedPass.PassTypeIdentifier,
                        PassesUpdatedSinceTag: "0"
                    )
                )
            ).Value;
            Assert.Contains(
                issuedPass.SerialNumber,
                initialSerials.SerialNumbers,
                StringComparer.Ordinal
            );

            var noneUpdated = (
                await queries.QueryAsync<
                    GetPassKitUpdatableSerialsQuery,
                    PassKitSerialNumbersResponse
                >(
                    new GetPassKitUpdatableSerialsQuery(
                        "ios-device-01",
                        issuedPass.PassTypeIdentifier,
                        PassesUpdatedSinceTag: initialSerials.LastUpdated
                    )
                )
            ).Value;
            Assert.Empty(noneUpdated.SerialNumbers);
        }

        await VerifyDynamicUpdatePushAndPassFetchAsync(provider, issuedPass, rawAppleToken);
    }

    private static async Task VerifyDynamicUpdatePushAndPassFetchAsync(
        ServiceProvider provider,
        IssuedWalletPassResponse issuedPass,
        string rawAppleToken
    )
    {
        var previousTag = issuedPass.UpdatedTag;
        await using (var updateScope = provider.CreateAsyncScope())
        {
            var cmds = updateScope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var updated = (
                await cmds.SendAsync<
                    UpdateIssuedWalletPassDynamicFieldsCommand,
                    IssuedWalletPassResponse
                >(
                    new UpdateIssuedWalletPassDynamicFieldsCommand(
                        issuedPass.Id,
                        new WalletPassDynamicFieldsDto(
                            HolderDisplayName: "Maya Rivera",
                            MembershipTier: "Gold",
                            PunchOrCreditBalance: 9,
                            NextShiftOrBookingSummary: "Sun 10:30 AM STEM Lab"
                        )
                    )
                )
            ).Value;
            Assert.Equal(2, updated.Revision);
            Assert.Equal(1, updated.RegisteredDeviceCount);
        }

        var pushSink = provider.GetRequiredService<InMemoryWalletPassPushNotifier>();
        Assert.Contains(pushSink.GetDispatchedNotifications(), n =>
            string.Equals(n.SerialNumber, issuedPass.SerialNumber, StringComparison.Ordinal)
            && string.Equals(n.PushToken, "apns-push-token-01", StringComparison.Ordinal)
        );

        await using (var fetchScope = provider.CreateAsyncScope())
        {
            var queries = fetchScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var cmds = fetchScope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var deltaSerials = (
                await queries.QueryAsync<
                    GetPassKitUpdatableSerialsQuery,
                    PassKitSerialNumbersResponse
                >(
                    new GetPassKitUpdatableSerialsQuery(
                        "ios-device-01",
                        issuedPass.PassTypeIdentifier,
                        PassesUpdatedSinceTag: previousTag
                    )
                )
            ).Value;
            _ = Assert.Single(deltaSerials.SerialNumbers);

            var latest = (
                await queries.QueryAsync<GetPassKitLatestPassQuery, PassKitLatestPassResponse>(
                    new GetPassKitLatestPassQuery(
                        issuedPass.PassTypeIdentifier,
                        issuedPass.SerialNumber,
                        rawAppleToken
                    )
                )
            ).Value;
            Assert.False(latest.NotModified);
            Assert.NotEmpty(latest.PkpassBytes);

            var notModified = (
                await queries.QueryAsync<GetPassKitLatestPassQuery, PassKitLatestPassResponse>(
                    new GetPassKitLatestPassQuery(
                        issuedPass.PassTypeIdentifier,
                        issuedPass.SerialNumber,
                        rawAppleToken,
                        IfModifiedSinceUtc: latest.LastModifiedUtc.AddSeconds(5)
                    )
                )
            ).Value;
            Assert.True(notModified.NotModified);

            var unreg = await cmds.SendAsync(
                new UnregisterPassKitDeviceCommand(
                    "ios-device-01",
                    issuedPass.PassTypeIdentifier,
                    issuedPass.SerialNumber,
                    rawAppleToken
                )
            );
            Assert.True(unreg.IsSuccess);
        }
    }

    public static async Task VerifyPersonMergeAndRevocationAsync(
        ServiceProvider provider,
        Guid issuedPassId,
        Guid retiredPersonId
    )
    {
        var survivingPersonId = Guid.CreateVersion7();

        await using (var mergeScope = provider.CreateAsyncScope())
        {
            var consumers = mergeScope.ServiceProvider.GetServices<
                IIntegrationEventConsumer<PersonMergedIntegrationEvent>
            >();
            foreach (var c in consumers)
            {
                await c.ConsumeAsync(
                    new PersonMergedIntegrationEvent(
                        EventId: Guid.CreateVersion7(),
                        OccurredOnUtc: DateTimeOffset.UtcNow,
                        SurvivingPersonId: survivingPersonId,
                        RetiredPersonId: retiredPersonId,
                        ExternalRecordGuids: [],
                        HadMultipleDistinctEmails: false
                    )
                );
            }
        }

        await using var verifyScope = provider.CreateAsyncScope();
        var queries = verifyScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var commands = verifyScope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var survivingPasses = (
            await queries.QueryAsync<
                GetIssuedWalletPassesQuery,
                IReadOnlyList<IssuedWalletPassResponse>
            >(new GetIssuedWalletPassesQuery(PersonId: survivingPersonId))
        ).Value;
        var repointed = Assert.Single(survivingPasses);
        Assert.Equal(issuedPassId, repointed.Id);

        var revoked = (
            await commands.SendAsync<RevokeIssuedWalletPassCommand, IssuedWalletPassResponse>(
                new RevokeIssuedWalletPassCommand(issuedPassId, "Card replaced")
            )
        ).Value;
        Assert.Equal(IssuedWalletPassStatus.Revoked, revoked.Status);

        var postRevokeScan = await queries.QueryAsync<
            ResolveKioskWalletPassScanQuery,
            KioskWalletPassScanResponse
        >(new ResolveKioskWalletPassScanQuery(revoked.ScanToken));
        Assert.True(postRevokeScan.IsFailure);
    }
}
