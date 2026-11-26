using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.Modules.Workflow.Tests.Infrastructure;

[Collection(WorkflowPostgresFixtures.Name)]
public sealed class W5S5WorkflowEngineIntegrationTests(WorkflowPostgresContainerFixture postgres)
{
    [Fact]
    public async Task Engine_AllModularPlugins_Branching_Suspension_AndOutbox_PersistEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        var clock = new AdvanceableTestClock(
            new DateTimeOffset(2026, 11, 25, 19, 0, 0, TimeSpan.Zero)
        );
        var webhookStub = new WorkflowStubHttpClientFactory();

        await using var provider = await WorkflowTestHostFactory.BuildAndMigrateAsync(
            conn,
            services =>
            {
                _ = services.Replace(ServiceDescriptor.Singleton<IClock>(clock));
                _ = services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(webhookStub));
            }
        );

        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var wfDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        var catalog = await queries.QueryAsync<
            GetWorkflowActionCatalogQuery,
            IReadOnlyList<WorkflowActionPluginDescriptor>
        >(new GetWorkflowActionCatalogQuery());
        Assert.Equal(11, catalog.Value.Count);

        var (groupId, accountId, opportunityId) =
            await WorkflowEngineTestSeedHelper.SeedCrossModuleTargetsAsync(commands);
        var wfType = await WorkflowEngineTestSeedHelper.CreateFullIntegrationWorkflowTypeAsync(
            commands,
            groupId,
            accountId,
            opportunityId
        );

        var launched = await LaunchAndVerifySuspendedPassAsync(
            commands,
            queries,
            wfType.Slug,
            groupId,
            webhookStub
        );

        clock.Advance(TimeSpan.FromSeconds(120));
        await WorkflowEngineAssertionHelper.ResumeAndVerifyCompletionAsync(
            commands,
            queries,
            wfDb,
            launched.Id
        );
    }

    private static async Task<WorkflowInstanceResponse> LaunchAndVerifySuspendedPassAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        string wfSlug,
        Guid groupId,
        WorkflowStubHttpClientFactory webhookStub
    )
    {
        var launched = await commands.SendAsync<LaunchWorkflowCommand, WorkflowInstanceResponse>(
            new LaunchWorkflowCommand(
                wfSlug,
                Name: "High-Tier Sponsorship Onboarding",
                InitialAttributes: WorkflowEngineTestSeedHelper.Map(
                    ("FirstName", "Maya"),
                    ("LastName", "Lin"),
                    ("Email", "maya.lin@example.org"),
                    ("PledgeAmount", "500.00")
                ),
                ProcessImmediately: true
            )
        );
        Assert.True(launched.IsSuccess);
        Assert.Equal(WorkflowInstanceStatus.Suspended, launched.Value.Status);
        Assert.NotEmpty(webhookStub.CapturedBodies);

        var createdPersonId = Guid.Parse(launched.Value.Attributes["PersonId"]!);
        var createdInvoiceId = Guid.Parse(launched.Value.Attributes["InvoiceId"]!);
        Assert.Equal("202", launched.Value.Attributes["WebhookLastStatusCode"]);
        Assert.Contains(
            "Maya",
            launched.Value.Attributes["RenderedWelcome"]!,
            StringComparison.Ordinal
        );
        Assert.Equal("2", launched.Value.Attributes["StepCounter"]);

        var members = await queries.QueryAsync<
            GetGroupMembersQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetGroupMembersQuery(groupId));
        Assert.Contains(members.Value, m => m.PersonId == createdPersonId);

        var invoice = await queries.QueryAsync<GetInvoiceByIdQuery, InvoiceResponse>(
            new GetInvoiceByIdQuery(createdInvoiceId)
        );
        Assert.True(invoice.IsSuccess);
        Assert.Equal(500.00m, invoice.Value.SubtotalAmount);

        return launched.Value;
    }
}
