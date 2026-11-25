using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Groups.Tests.Fixtures;

internal static class CrmGroupsTestSeeder
{
    public static async Task<Guid> CreatePersonAsync(
        ICommandDispatcher commands,
        string firstName,
        string lastName,
        PersonConnectionStatus status,
        IReadOnlyDictionary<string, string?>? attributes = null
    )
    {
        var result = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                FirstName: firstName,
                LastName: lastName,
                ConnectionStatus: status,
                BirthYear: 1990,
                BirthMonth: 5,
                BirthDay: 15,
                Attributes: attributes
            )
        );
        Assert.True(result.IsSuccess);
        return result.Value.Id;
    }

    public static async Task<(
        Guid ClearPersonId,
        Guid MissingPersonId
    )> SeedBackgroundCheckPairAsync(ICommandDispatcher commands)
    {
        var clearPersonId = await CreatePersonAsync(
            commands,
            "Grace",
            "Miller",
            PersonConnectionStatus.Member,
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["covenant_signed"] = "true" }
        );
        var missingPersonId = await CreatePersonAsync(
            commands,
            "Ethan",
            "Cole",
            PersonConnectionStatus.Attendee
        );

        var checkReq = await commands.SendAsync<
            RequestBackgroundCheckCommand,
            BackgroundCheckResponse
        >(
            new RequestBackgroundCheckCommand(
                clearPersonId,
                InMemoryBackgroundCheckProvider.Key,
                "kids-volunteer"
            )
        );
        Assert.True(checkReq.IsSuccess);

        var payload =
            $$"""{"requestId":"{{checkReq.Value.RequestId}}","status":"clear","responseId":"doc_kids_1"}""";
        var webhook = await commands.SendAsync<
            ProcessBackgroundCheckWebhookCommand,
            BackgroundCheckResponse
        >(
            new ProcessBackgroundCheckWebhookCommand(
                InMemoryBackgroundCheckProvider.Key,
                payload,
                InMemoryBackgroundCheckProvider.ComputeSignature(payload)
            )
        );
        Assert.True(webhook.IsSuccess);

        return (clearPersonId, missingPersonId);
    }
}
