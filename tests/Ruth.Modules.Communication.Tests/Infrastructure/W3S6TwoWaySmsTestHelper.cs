using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

internal static class W3S6TwoWaySmsTestHelper
{
    public static async Task VerifyTwoWaySmsAndPersonMergeAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid retiredPersonId,
        Guid survivingPersonId
    )
    {
        var orgPhone = (
            await commands.SendAsync<CreateSystemPhoneNumberCommand, SystemPhoneNumberResponse>(
                new CreateSystemPhoneNumberCommand("Inbox Support SMS", "+15552001000")
            )
        ).Value;

        var helpRes = (
            await commands.SendAsync<ProcessInboundSmsWebhookCommand, InboundSmsProcessResponse>(
                new ProcessInboundSmsWebhookCommand(
                    "+15559008000",
                    orgPhone.Number,
                    "HELP",
                    SenderDisplayName: "Morgan Member"
                )
            )
        ).Value;
        Assert.Equal(1, helpRes.MatchedPipelineActions);
        _ = Assert.Single(helpRes.AutomatedRepliesSent);

        var stopRes = (
            await commands.SendAsync<ProcessInboundSmsWebhookCommand, InboundSmsProcessResponse>(
                new ProcessInboundSmsWebhookCommand("+15559008000", orgPhone.Number, "STOP")
            )
        ).Value;
        Assert.True(stopRes.OptOutStateChanged);

        var blockedReply = await commands.SendAsync<
            SendSmsConversationReplyCommand,
            SmsConversationMessageResponse
        >(new SendSmsConversationReplyCommand(orgPhone.Id, "+15559008000", "Can we follow up?"));
        Assert.True(blockedReply.IsFailure);

        _ = await commands.SendAsync<ProcessInboundSmsWebhookCommand, InboundSmsProcessResponse>(
            new ProcessInboundSmsWebhookCommand("+15559008000", orgPhone.Number, "START")
        );

        var allowedReply = await commands.SendAsync<
            SendSmsConversationReplyCommand,
            SmsConversationMessageResponse
        >(
            new SendSmsConversationReplyCommand(
                orgPhone.Id,
                "+15559008000",
                "Welcome back! How can we help?",
                RepliedByPersonId: retiredPersonId
            )
        );
        Assert.True(allowedReply.IsSuccess);

        var thread = (
            await queries.QueryAsync<
                GetSmsConversationThreadQuery,
                IReadOnlyList<SmsConversationMessageResponse>
            >(new GetSmsConversationThreadQuery(orgPhone.Id, "+15559008000"))
        ).Value;
        Assert.Equal(7, thread.Count);

        await W3S5CrossModuleEventsTestHelper.ConsumePersonMergeAsync(
            sp,
            retiredPersonId,
            survivingPersonId
        );

        var updatedThread = (
            await queries.QueryAsync<
                GetSmsConversationThreadQuery,
                IReadOnlyList<SmsConversationMessageResponse>
            >(new GetSmsConversationThreadQuery(orgPhone.Id, "+15559008000"))
        ).Value;
        Assert.Contains(updatedThread, m => m.RepliedByPersonId == survivingPersonId);
    }
}
