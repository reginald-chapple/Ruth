using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Slices.BulkCommunications;
using Ruth.Modules.Communication.Slices.Flows;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

internal static class W7S5CommunicationRlsTestHelper
{
    public static async Task AssertAllCommsTablesHaveForceRlsAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = 'comms' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Assert.True(reader.GetBoolean(1), $"Missing RLS on comms.{reader.GetString(0)}");
            Assert.True(reader.GetBoolean(2), $"Missing FORCE RLS on comms.{reader.GetString(0)}");
        }

        Assert.True(count >= 16);
    }

    public static async Task VerifySandboxSmsAndScheduledJobsAsync(
        ServiceProvider provider,
        TenantId tenantA
    )
    {
        using (
            AmbientTenantContext.BeginScope(
                tenantA,
                "sandbox-charity",
                TenantLifecycleStage.Sandbox
            )
        )
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var phone = (
                await commands.SendAsync<CreateSystemPhoneNumberCommand, SystemPhoneNumberResponse>(
                    new CreateSystemPhoneNumberCommand("Support Line", "+15553001000")
                )
            ).Value;
            var blockedSms = await commands.SendAsync<
                SendSmsConversationReplyCommand,
                SmsConversationMessageResponse
            >(new SendSmsConversationReplyCommand(phone.Id, "+15559990000", "Hello"));
            Assert.Equal(BulkCommunicationErrors.TenantNotLive.Code, blockedSms.PrimaryError.Code);

            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var bulkJob = new BulkCommunicationDispatchJob(scopeFactory);
            var flowJob = new CommunicationFlowAdvancementJob(scopeFactory);
            Assert.Equal(JobExecutionStatus.Success, (await bulkJob.ExecuteAsync()).Status);
            Assert.Equal(JobExecutionStatus.Success, (await flowJob.ExecuteAsync()).Status);
        }
    }

    internal sealed class FakeExternalEmailTransport : ICommunicationTransport
    {
        public string TransportKey => "fake-live-email";

        public string DisplayName => "Fake Live Email Gateway";

        public CommunicationChannel Channel => CommunicationChannel.Email;

        public bool IsConfigured => true;

        public Task<Result<TransportDeliveryReceipt>> SendAsync(
            OutboundTransportMessage message,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(
                Result.Success(
                    new TransportDeliveryReceipt(
                        TransportKey,
                        CommunicationChannel.Email,
                        "msg-live-1",
                        DateTimeOffset.UtcNow,
                        IsSimulated: false
                    )
                )
            );
        }
    }
}
