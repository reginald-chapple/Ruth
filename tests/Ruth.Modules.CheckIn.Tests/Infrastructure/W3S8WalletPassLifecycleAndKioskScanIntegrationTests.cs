using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.CheckIn.Tests.Fixtures;
using Ruth.Providers.WalletPasses;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

[Collection(CheckInPostgresFixtures.Name)]
public sealed class W3S8WalletPassLifecycleAndKioskScanIntegrationTests(
    CheckInPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task WalletPassIssuance_ClaimBundle_Sub50MsKioskScan_PassKitProtocol_Outbox_AndMerge_WorkAgainstPostgres18()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CheckInTestHostFactory.BuildAndMigrateAsync(cs, services =>
        {
            var cfg = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            _ = services.AddWalletPassProviders(cfg);
        });

        var scheduleId = Guid.CreateVersion7();
        var roomAId = Guid.CreateVersion7();
        var roomBId = Guid.CreateVersion7();
        Guid personId;
        Guid configId;

        await using (var seedScope = provider.CreateAsyncScope())
        {
            var seedCommands = seedScope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
            var (_, _, childId) = await W3S3CheckInTestSeedHelper.SeedHouseholdInCrmAsync(
                seedCommands
            );
            personId = childId;
            var groupId = await W3S3CheckInTestSeedHelper.CreateLinkedYouthGroupAsync(seedCommands);
            var (cid, _) = await W3S3CheckInProfileSeedHelper.ConfigureCheckInProfileAsync(
                seedCommands,
                groupId,
                scheduleId,
                roomAId,
                roomBId
            );
            configId = cid;
        }

        var issuedResult = await IssuePassAndVerifyOutboxAndClaimAsync(provider, personId);
        await VerifySub50MsKioskScanAndKioskSearchHookAsync(
            provider,
            issuedResult.Pass,
            configId,
            scheduleId
        );
        await W3S8PassKitProtocolTestHelper.VerifyPassKitDeviceLifecycleAndDynamicPushUpdatesAsync(
            provider,
            issuedResult.Pass
        );
        await W3S8PassKitProtocolTestHelper.VerifyPersonMergeAndRevocationAsync(
            provider,
            issuedResult.Pass.Id,
            personId
        );
    }

    private static async Task<IssueWalletPassResultResponse> IssuePassAndVerifyOutboxAndClaimAsync(
        ServiceProvider provider,
        Guid personId
    )
    {
        await using var scope = provider.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var db = scope.ServiceProvider.GetRequiredService<CheckInDbContext>();

        var templates = (
            await queries.QueryAsync<
                GetWalletPassTemplatesQuery,
                IReadOnlyList<WalletPassTemplateResponse>
            >(new GetWalletPassTemplatesQuery(ActiveOnly: true))
        ).Value;
        var seededTemplate = Assert.Single(templates, static t =>
            string.Equals(t.Key, "member-access-pass", StringComparison.Ordinal)
        );

        var issued = (
            await commands.SendAsync<IssueWalletPassCommand, IssueWalletPassResultResponse>(
                new IssueWalletPassCommand(
                    seededTemplate.Id,
                    personId,
                    new WalletPassDynamicFieldsDto(
                        MembershipTier: "Family",
                        PunchOrCreditBalance: 10,
                        NextShiftOrBookingSummary: "Sat 9:00 AM Youth STEM"
                    ),
                    SendClaimNotification: true,
                    RecipientEmail: "parent@example.org"
                )
            )
        ).Value;

        Assert.True(issued.NotificationQueued);
        Assert.NotEmpty(issued.ClaimToken);
        Assert.True(await db.OutboxMessages.AnyAsync());

        var claimBundle = (
            await queries.QueryAsync<ClaimWalletPassQuery, WalletPassClaimBundleResponse>(
                new ClaimWalletPassQuery(issued.ClaimToken)
            )
        ).Value;
        Assert.Equal(issued.Pass.SerialNumber, claimBundle.Pass.SerialNumber);
        Assert.Contains(
            issued.Pass.ScanToken,
            claimBundle.PreviewCard.PreviewSvgMarkup,
            StringComparison.Ordinal
        );

        var badClaim = await queries.QueryAsync<
            ClaimWalletPassQuery,
            WalletPassClaimBundleResponse
        >(new ClaimWalletPassQuery("invalid-claim-token"));
        Assert.True(badClaim.IsFailure);

        return issued;
    }

    private static async Task VerifySub50MsKioskScanAndKioskSearchHookAsync(
        ServiceProvider provider,
        IssuedWalletPassResponse pass,
        Guid configId,
        Guid scheduleId
    )
    {
        await using var warmScope = provider.CreateAsyncScope();
        var warmQueries = warmScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        _ = await warmQueries.QueryAsync<
            ResolveKioskWalletPassScanQuery,
            KioskWalletPassScanResponse
        >(new ResolveKioskWalletPassScanQuery(pass.ScanToken));

        await using var timedScope = provider.CreateAsyncScope();
        var queries = timedScope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var scan = (
            await queries.QueryAsync<ResolveKioskWalletPassScanQuery, KioskWalletPassScanResponse>(
                new ResolveKioskWalletPassScanQuery(pass.ScanToken, configId, scheduleId)
            )
        ).Value;

        Assert.Equal(pass.PersonId, scan.PersonId);
        Assert.True(
            scan.ResolutionMilliseconds < 50.0,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Expected sub-50ms Kiosk scan lookup, actual {scan.ResolutionMilliseconds} ms"
            )
        );
        Assert.NotNull(scan.KioskCandidates);
        Assert.NotEmpty(scan.KioskCandidates.Families);
        Assert.Equal("WalletPass", scan.KioskCandidates.Families[0].MatchKind);
    }
}
