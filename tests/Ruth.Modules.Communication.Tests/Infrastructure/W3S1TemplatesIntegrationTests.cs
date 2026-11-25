using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Communication.Infrastructure.Templating;
using Ruth.Modules.Communication.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Infrastructure;

[Collection(CommunicationPostgresFixtures.Name)]
public sealed class W3S1TemplatesIntegrationTests(
    CommunicationPostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task Snippet_Crud_And_System_Delete_Guard_Work_Against_Postgres18()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CommunicationTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var created = await commands.SendAsync<
            CreateTemplateSnippetCommand,
            TemplateSnippetResponse
        >(
            new CreateTemplateSnippetCommand(
                "Giving Callout",
                "giving_callout",
                "<strong>{{ label }}: {{ Gift.Amount | currency }}</strong>",
                Description: "Highlights a donor gift amount",
                Documentation: "Accepts label parameter and reads Gift.Amount",
                OutputMode: TemplateOutputMode.Html,
                DefaultParameters: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["label"] = "Total Gift",
                },
                CategoryIds: [Guid.CreateVersion7()],
                IsActive: true
            )
        );
        Assert.True(created.IsSuccess);
        Assert.Equal("giving_callout", created.Value.Key);

        var duplicate = await commands.SendAsync<
            CreateTemplateSnippetCommand,
            TemplateSnippetResponse
        >(new CreateTemplateSnippetCommand("Dup", "GIVING_CALLOUT", "<p>dup</p>"));
        Assert.True(duplicate.IsFailure);
        Assert.Equal(
            CommunicationTemplateErrors.SnippetKeyDuplicate.Code,
            duplicate.PrimaryError.Code
        );

        var list = (
            await queries.QueryAsync<
                GetTemplateSnippetsQuery,
                IReadOnlyList<TemplateSnippetResponse>
            >(new GetTemplateSnippetsQuery(ActiveOnly: true, Search: "giving"))
        ).Value;
        var single = Assert.Single(list);
        Assert.Equal(created.Value.Id, single.Id);

        var fetched = (
            await queries.QueryAsync<GetTemplateSnippetByIdQuery, TemplateSnippetResponse>(
                new GetTemplateSnippetByIdQuery(created.Value.Id)
            )
        ).Value;
        Assert.Equal("Giving Callout", fetched.Name);

        var updated = (
            await commands.SendAsync<UpdateTemplateSnippetCommand, TemplateSnippetResponse>(
                new UpdateTemplateSnippetCommand(
                    created.Value.Id,
                    "Giving Callout v2",
                    "giving_callout",
                    "<em>{{ label }}</em>",
                    IsActive: false
                )
            )
        ).Value;
        Assert.False(updated.IsActive);

        var deleted = await commands.SendAsync<DeleteTemplateSnippetCommand, bool>(
            new DeleteTemplateSnippetCommand(created.Value.Id)
        );
        Assert.True(deleted.IsSuccess);

        var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        var sysSnippet = TemplateSnippet
            .Create("System Legal Footer", "system_legal_footer", "<p>Legal</p>", isSystem: true)
            .Value;
        _ = dbContext.TemplateSnippets.Add(sysSnippet);
        _ = await dbContext.SaveChangesAsync();

        var deleteSystem = await commands.SendAsync<DeleteTemplateSnippetCommand, bool>(
            new DeleteTemplateSnippetCommand(sysSnippet.Id.Value)
        );
        Assert.True(deleteSystem.IsFailure);
        Assert.Equal(
            CommunicationTemplateErrors.SystemSnippetCannotBeDeleted.Code,
            deleteSystem.PrimaryError.Code
        );
    }

    [Fact]
    public async Task MergePreview_And_RazorPreview_Compose_Persisted_Snippets_Against_Postgres18()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CommunicationTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var created = (
            await commands.SendAsync<CreateTemplateSnippetCommand, TemplateSnippetResponse>(
                new CreateTemplateSnippetCommand(
                    "Giving Callout",
                    "giving_callout",
                    "<strong>{{ label }}: {{ Gift.Amount | currency }}</strong>",
                    DefaultParameters: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["label"] = "Total Gift",
                    }
                )
            )
        ).Value;

        var mergePreview = (
            await commands.SendAsync<PreviewMergeTemplateCommand, TemplatePreviewResponse>(
                new PreviewMergeTemplateCommand(
                    "<p>Hi {{ Person.NickName | default:'Friend' }}, {{ snippet:giving_callout label=\"Your Gift\" }}</p>",
                    /*lang=json,strict*/
                    """{"Person":{"NickName":"Jordan"},"Gift":{"Amount":250.00}}""",
                    TemplateOutputMode.Html
                )
            )
        ).Value;
        Assert.Equal(1, mergePreview.SnippetsExpanded);
        Assert.Equal(
            "<p>Hi Jordan, <strong>Your Gift: $250.00</strong></p>",
            mergePreview.RenderedContent
        );

        var razorList = (
            await queries.QueryAsync<
                GetRegisteredRazorTemplatesQuery,
                IReadOnlyList<RazorTemplateSummaryResponse>
            >(new GetRegisteredRazorTemplatesQuery(Category: "Email"))
        ).Value;
        var emailDescriptor = Assert.Single(razorList);
        Assert.Equal(BuiltInRazorTemplatesCatalog.StandardEmailKey, emailDescriptor.Key);

        var razorPreview = (
            await commands.SendAsync<PreviewRazorTemplateCommand, TemplatePreviewResponse>(
                new PreviewRazorTemplateCommand(
                    BuiltInRazorTemplatesCatalog.StandardEmailKey,
                    new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["OrganizationName"] = "Grace Community",
                        ["Subject"] = "Thank You",
                        ["Heading"] = "Thank You, {{ Person.NickName }}!",
                        ["BodyHtml"] = "<p>We received {{ snippet:giving_callout }}.</p>",
                        ["ActionLabel"] = "View Statement",
                        ["ActionUrl"] = "https://grace.example/statements",
                    },
                    /*lang=json,strict*/
                    """{"Person":{"NickName":"Jordan"},"Gift":{"Amount":250.00}}"""
                )
            )
        ).Value;
        Assert.Equal(1, razorPreview.SnippetsExpanded);
        Assert.Contains(
            "Thank You, Jordan!",
            razorPreview.RenderedContent,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "<strong>Total Gift: $250.00</strong>",
            razorPreview.RenderedContent,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "https://grace.example/statements",
            razorPreview.RenderedContent,
            StringComparison.Ordinal
        );

        _ = await commands.SendAsync<UpdateTemplateSnippetCommand, TemplateSnippetResponse>(
            new UpdateTemplateSnippetCommand(
                created.Id,
                "Giving Callout",
                "giving_callout",
                "<em>{{ label }}</em>",
                IsActive: false
            )
        );
        var inactiveMerge = await commands.SendAsync<
            PreviewMergeTemplateCommand,
            TemplatePreviewResponse
        >(new PreviewMergeTemplateCommand("{{ snippet:giving_callout }}"));
        Assert.True(inactiveMerge.IsFailure);
        Assert.Equal("Communication.UnknownSnippet", inactiveMerge.PrimaryError.Code);
    }
}
