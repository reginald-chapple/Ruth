using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Tests.Fixtures;
using Ruth.Providers.CommunicationTransports;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

[Collection(CommunicationPostgresFixtures.Name)]
public sealed class W3S5SystemNotificationsAndEventsIntegrationTests(
    CommunicationPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task SeededNotifications_EventWiring_MultiChannelDispatch_AndPersonMerge_WorkAgainstPostgres18()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CommunicationTestHostFactory.BuildAndMigrateAsync(
            cs,
            services =>
            {
                var cfg = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                _ = services.AddCommunicationTransportProviders(cfg);
            }
        );

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var transports = (
            await queries.QueryAsync<
                GetCommunicationTransportsQuery,
                IReadOnlyList<CommunicationTransportStatusResponse>
            >(new GetCommunicationTransportsQuery())
        ).Value;
        Assert.Equal(7, transports.Count);

        var sysComms = (
            await queries.QueryAsync<
                GetSystemCommunicationsQuery,
                IReadOnlyList<SystemCommunicationResponse>
            >(new GetSystemCommunicationsQuery(ActiveOnly: true))
        ).Value;
        Assert.Equal(6, sysComms.Count);
        Assert.Contains(sysComms, static c =>
            string.Equals(c.Key, "finance.payment-or-gift-receipt", StringComparison.Ordinal)
        );

        var retiredPersonId = Guid.CreateVersion7();
        var survivingPersonId = Guid.CreateVersion7();
        var phone = (
            await commands.SendAsync<CreateSystemPhoneNumberCommand, SystemPhoneNumberResponse>(
                new CreateSystemPhoneNumberCommand(
                    "Member Operations SMS",
                    "(555) 777-8899",
                    IsSmsEnabled: true,
                    AssignedToPersonId: retiredPersonId
                )
            )
        ).Value;
        Assert.Equal("+15557778899", phone.Number);

        var device = (
            await commands.SendAsync<RegisterPersonalDeviceCommand, PersonalDeviceResponse>(
                new RegisterPersonalDeviceCommand(
                    PersonalDevicePlatform.Ios,
                    "apns-member-device-token-00112233",
                    PersonId: retiredPersonId,
                    DeviceName: "Member iPhone"
                )
            )
        ).Value;
        Assert.Equal(retiredPersonId, device.PersonId);

        await W3S5CrossModuleEventsTestHelper.ConsumeAllSampleEventsAsync(
            scope.ServiceProvider,
            retiredPersonId
        );
        await VerifyDeliveriesAndExportHtmlAsync(queries);

        await W3S5CrossModuleEventsTestHelper.ConsumePersonMergeAsync(
            scope.ServiceProvider,
            retiredPersonId,
            survivingPersonId
        );

        var repointedDevices = (
            await queries.QueryAsync<
                GetPersonalDevicesQuery,
                IReadOnlyList<PersonalDeviceResponse>
            >(new GetPersonalDevicesQuery(PersonId: survivingPersonId, ActiveOnly: true))
        ).Value;
        _ = Assert.Single(repointedDevices);
    }

    private static async Task VerifyDeliveriesAndExportHtmlAsync(IQueryDispatcher queries)
    {
        var logs = (
            await queries.QueryAsync<
                GetSystemNotificationLogsQuery,
                IReadOnlyList<SystemNotificationLogResponse>
            >(new GetSystemNotificationLogsQuery(Limit: 50))
        ).Value;
        Assert.True(logs.Count >= 10);

        var captured = (
            await queries.QueryAsync<
                GetInMemorySentMessagesQuery,
                IReadOnlyList<InMemoryCapturedMessageResponse>
            >(new GetInMemorySentMessagesQuery(Channel: CommunicationChannel.Email, Limit: 20))
        ).Value;
        Assert.True(captured.Count >= 5);

        var exportDir = Path.Combine(FindRepoRoot(), "artifacts", "htmlhint");
        _ = Directory.CreateDirectory(exportDir);
        foreach (var email in captured)
        {
            if (string.IsNullOrWhiteSpace(email.HtmlBody))
            {
                continue;
            }

            var safeKey = (email.SystemCommunicationKey ?? "email").Replace('.', '-');
            var path = Path.Combine(exportDir, $"{safeKey}.html");
            await File.WriteAllTextAsync(path, email.HtmlBody);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ruth.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
