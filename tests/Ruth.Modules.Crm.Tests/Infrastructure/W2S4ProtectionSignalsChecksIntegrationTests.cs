using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W2S4ProtectionSignalsChecksIntegrationTests(CrmPostgresContainerFixture fixture)
{
    [Fact]
    public async Task Signals_Assessments_BackgroundChecks_AndBadges_WorkEndToEnd()
    {
        var conn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(
            conn,
            checkrWebhookSecret: "checkr-test-secret"
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var personId = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(
                    "Hannah",
                    "Brooks",
                    Email: "hannah@example.org",
                    ConnectionStatus: PersonConnectionStatus.Member,
                    BirthYear: 1992,
                    BirthMonth: 8,
                    BirthDay: 20
                )
            )
        )
            .Value
            .Id;

        await VerifySignalsAsync(commands, queries, personId);
        await VerifyAssessmentsAsync(commands, queries, personId);
        await VerifyBackgroundCheckWebhookAsync(commands, personId);
        await VerifyBadgesAsync(queries, personId);
    }

    private static async Task VerifySignalsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        var lowType = (
            await commands.SendAsync<CreateSignalTypeCommand, SignalTypeResponse>(
                new CreateSignalTypeCommand("First-Time Guest", "#0078D4", "star", Order: 10)
            )
        ).Value;
        var highType = (
            await commands.SendAsync<CreateSignalTypeCommand, SignalTypeResponse>(
                new CreateSignalTypeCommand("Medical Allergy", "#D13438", "warning", Order: 1)
            )
        ).Value;

        _ = await commands.SendAsync<AddPersonSignalCommand, PersonSignalResponse>(
            new AddPersonSignalCommand(personId, lowType.Id, personId, "Welcome")
        );
        var highSignal = (
            await commands.SendAsync<AddPersonSignalCommand, PersonSignalResponse>(
                new AddPersonSignalCommand(personId, highType.Id, personId, "Peanut allergy")
            )
        ).Value;

        var afterSignal = (
            await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(personId)
            )
        ).Value;
        Assert.Equal(highSignal.Id, afterSignal.TopSignalId);
        Assert.Equal("#D13438", afterSignal.TopSignalColor);
    }

    private static async Task VerifyAssessmentsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid personId
    )
    {
        var types = (
            await queries.QueryAsync<
                GetAssessmentTypesQuery,
                IReadOnlyList<AssessmentTypeResponse>
            >(new GetAssessmentTypesQuery(ActiveOnly: true))
        ).Value;
        var discType = types.First(static t =>
            string.Equals(t.Key, "disc", StringComparison.Ordinal)
        );

        var requested = (
            await commands.SendAsync<RequestAssessmentCommand, AssessmentResponse>(
                new RequestAssessmentCommand(personId, discType.Id)
            )
        ).Value;
        var completed = (
            await commands.SendAsync<CompleteAssessmentCommand, AssessmentResponse>(
                new CompleteAssessmentCommand(
                    requested.Id,
                    /*lang=json,strict*/
                    "{\"style\":\"D/I\"}",
                    "DISC: D/I"
                )
            )
        ).Value;
        Assert.Equal(AssessmentStatus.Complete, completed.Status);

        var cooldownBlocked = await commands.SendAsync<
            RequestAssessmentCommand,
            AssessmentResponse
        >(new RequestAssessmentCommand(personId, discType.Id));
        Assert.True(cooldownBlocked.IsFailure);
        Assert.Equal(CrmErrors.AssessmentCooldownActive, cooldownBlocked.PrimaryError);
    }

    private static async Task VerifyBackgroundCheckWebhookAsync(
        ICommandDispatcher commands,
        Guid personId
    )
    {
        var check = (
            await commands.SendAsync<RequestBackgroundCheckCommand, BackgroundCheckResponse>(
                new RequestBackgroundCheckCommand(
                    personId,
                    InMemoryBackgroundCheckProvider.Key,
                    "standard-volunteer"
                )
            )
        ).Value;
        Assert.Equal(BackgroundCheckStatus.InvitationSent, check.Status);

        var webhookBody =
            $$"""{"requestId":"{{check.RequestId}}","status":"clear","responseId":"doc_55"}""";
        var badSig = await commands.SendAsync<
            ProcessBackgroundCheckWebhookCommand,
            BackgroundCheckResponse
        >(
            new ProcessBackgroundCheckWebhookCommand(
                InMemoryBackgroundCheckProvider.Key,
                webhookBody,
                "invalid-sig"
            )
        );
        Assert.Equal(CrmErrors.BackgroundCheckWebhookInvalidSignature, badSig.PrimaryError);

        var goodSig = InMemoryBackgroundCheckProvider.ComputeSignature(webhookBody);
        var webhookOk = (
            await commands.SendAsync<ProcessBackgroundCheckWebhookCommand, BackgroundCheckResponse>(
                new ProcessBackgroundCheckWebhookCommand(
                    InMemoryBackgroundCheckProvider.Key,
                    webhookBody,
                    goodSig
                )
            )
        ).Value;
        Assert.Equal(BackgroundCheckStatus.ReportClear, webhookOk.Status);
        Assert.False(webhookOk.RecordFound);
        Assert.Equal("Candidate Pass", webhookOk.Recommendation);
    }

    private static async Task VerifyBadgesAsync(IQueryDispatcher queries, Guid personId)
    {
        var badges = (
            await queries.QueryAsync<
                GetEvaluatedPersonBadgesQuery,
                IReadOnlyList<EvaluatedPersonBadgeResponse>
            >(new GetEvaluatedPersonBadgesQuery(personId))
        ).Value;

        Assert.Contains(badges, static b =>
            string.Equals(b.ComponentKey, "top-signal", StringComparison.Ordinal)
        );
        Assert.Contains(badges, static b =>
            string.Equals(b.ComponentKey, "assessment-summary", StringComparison.Ordinal)
            && string.Equals(b.Value, "DISC: D/I", StringComparison.Ordinal)
        );
        Assert.Contains(badges, static b =>
            string.Equals(b.ComponentKey, "background-check", StringComparison.Ordinal)
            && string.Equals(b.Value, "Pass", StringComparison.Ordinal)
        );
    }
}
