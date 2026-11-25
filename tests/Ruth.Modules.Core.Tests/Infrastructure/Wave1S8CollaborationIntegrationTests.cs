using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class Wave1S8CollaborationIntegrationTests(
    CorePostgresContainerFixture postgresFixture
)
{
    [Fact]
    public async Task HistoryLog_And_Threaded_Notes_With_Watches_Work_EndToEnd()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var targetEntityId = Guid.CreateVersion7();

        var campusType = (
            await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
                new GetEntityTypeByKeyQuery(Campus.EntityTypeKeyName)
            )
        ).Value;

        _ = await commands.SendAsync<RecordHistoryLogCommand, HistoryLogEntryResponse>(
            new RecordHistoryLogCommand(
                campusType.Id,
                targetEntityId,
                "Modify",
                ValueName: "TaxId",
                OldValue: "11-1111111",
                NewValue: "22-2222222",
                IsSensitive: true,
                ChangedByUserId: userA
            )
        );
        var history = (
            await queries.QueryAsync<GetHistoryLogQuery, IReadOnlyList<HistoryLogEntryResponse>>(
                new GetHistoryLogQuery(campusType.Id, targetEntityId)
            )
        ).Value;
        var entry = Assert.Single(history);
        Assert.Null(entry.NewValue);
        Assert.Contains("Sensitive", entry.Summary, StringComparison.Ordinal);

        var noteType = (
            await commands.SendAsync<CreateNoteTypeCommand, NoteTypeResponse>(
                new CreateNoteTypeCommand(
                    campusType.Id,
                    "Care Notes",
                    AllowsReplies: true,
                    MaxReplyDepth: 1,
                    AllowsWatching: true,
                    AutoWatchAuthors: true
                )
            )
        ).Value;

        var rootNote = (
            await commands.SendAsync<CreateNoteCommand, NoteResponse>(
                new CreateNoteCommand(
                    noteType.Id,
                    targetEntityId,
                    "Followed up",
                    AuthorUserId: userA,
                    IsPinned: true
                )
            )
        ).Value;
        var reply = (
            await commands.SendAsync<CreateNoteCommand, NoteResponse>(
                new CreateNoteCommand(
                    noteType.Id,
                    targetEntityId,
                    "Confirmed meal",
                    ParentNoteId: rootNote.Id,
                    AuthorUserId: userB
                )
            )
        ).Value;
        var tooDeep = await commands.SendAsync<CreateNoteCommand, NoteResponse>(
            new CreateNoteCommand(
                noteType.Id,
                targetEntityId,
                "Too deep",
                ParentNoteId: reply.Id,
                AuthorUserId: userA
            )
        );
        Assert.True(tooDeep.IsFailure);

        _ = await commands.SendAsync<CreateNoteCommand, NoteResponse>(
            new CreateNoteCommand(
                noteType.Id,
                targetEntityId,
                "Private note",
                IsPrivateNote: true,
                AuthorUserId: userA
            )
        );
        var timelineForA = (
            await queries.QueryAsync<GetNotesTimelineQuery, IReadOnlyList<NoteResponse>>(
                new GetNotesTimelineQuery(targetEntityId, noteType.Id, ViewerUserId: userA)
            )
        ).Value;
        var timelineForB = (
            await queries.QueryAsync<GetNotesTimelineQuery, IReadOnlyList<NoteResponse>>(
                new GetNotesTimelineQuery(targetEntityId, noteType.Id, ViewerUserId: userB)
            )
        ).Value;
        Assert.Equal(2, timelineForA.Count);
        var onlyVisibleToB = Assert.Single(timelineForB);
        _ = Assert.Single(onlyVisibleToB.Replies);

        _ = await commands.SendAsync<SetNoteWatchCommand, NoteWatchResponse>(
            new SetNoteWatchCommand(
                userB,
                IsWatching: true,
                AllowOverride: false,
                NoteTypeId: noteType.Id
            )
        );
        var unwatchBlocked = await commands.SendAsync<SetNoteWatchCommand, NoteWatchResponse>(
            new SetNoteWatchCommand(userB, IsWatching: false, NoteTypeId: noteType.Id)
        );
        Assert.True(unwatchBlocked.IsFailure);
    }

    [Fact]
    public async Task Tags_EntitySets_And_PersonPreferences_Work_EndToEnd()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var userA = Guid.CreateVersion7();
        var userB = Guid.CreateVersion7();
        var targetEntityId = Guid.CreateVersion7();

        var campusType = (
            await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
                new GetEntityTypeByKeyQuery(Campus.EntityTypeKeyName)
            )
        ).Value;

        var orgTag = (
            await commands.SendAsync<CreateTagCommand, TagResponse>(
                new CreateTagCommand("First-Time Guest", campusType.Id)
            )
        ).Value;
        var personalTag = (
            await commands.SendAsync<CreateTagCommand, TagResponse>(
                new CreateTagCommand("My Followups", campusType.Id, OwnerUserId: userA)
            )
        ).Value;

        _ = await commands.SendAsync<TagEntityCommand, bool>(
            new TagEntityCommand(orgTag.Id, campusType.Id, targetEntityId, userA)
        );
        _ = await commands.SendAsync<TagEntityCommand, bool>(
            new TagEntityCommand(personalTag.Id, campusType.Id, targetEntityId, userA)
        );
        var personalDenied = await commands.SendAsync<TagEntityCommand, bool>(
            new TagEntityCommand(personalTag.Id, campusType.Id, Guid.CreateVersion7(), userB)
        );
        Assert.True(personalDenied.IsFailure);

        var tagsForA = (
            await queries.QueryAsync<GetEntityTagsQuery, IReadOnlyList<TagResponse>>(
                new GetEntityTagsQuery(campusType.Id, targetEntityId, userA)
            )
        ).Value;
        var tagsForB = (
            await queries.QueryAsync<GetEntityTagsQuery, IReadOnlyList<TagResponse>>(
                new GetEntityTagsQuery(campusType.Id, targetEntityId, userB)
            )
        ).Value;
        Assert.Equal(2, tagsForA.Count);
        _ = Assert.Single(tagsForB);

        var savedSet = (
            await commands.SendAsync<CreateEntitySetCommand, EntitySetResponse>(
                new CreateEntitySetCommand(
                    "Annual Audit Selection",
                    campusType.Id,
                    [targetEntityId, Guid.CreateVersion7()],
                    ExpireInMinutes: null
                )
            )
        ).Value;
        Assert.Equal(2, savedSet.Items.Count);
        var activeSets = (
            await queries.QueryAsync<GetEntitySetsQuery, IReadOnlyList<EntitySetResponse>>(
                new GetEntitySetsQuery(campusType.Id)
            )
        ).Value;
        _ = Assert.Single(activeSets);

        _ = await commands.SendAsync<SetPersonPreferenceCommand, PersonPreferenceResponse>(
            new SetPersonPreferenceCommand(userA, "grid-campus-page-size", "50", IsEnduring: true)
        );
        _ = await commands.SendAsync<SetPersonPreferenceCommand, PersonPreferenceResponse>(
            new SetPersonPreferenceCommand(userA, "grid-campus-page-size", "100", IsEnduring: true)
        );
        var prefs = (
            await queries.QueryAsync<
                GetPersonPreferencesQuery,
                IReadOnlyList<PersonPreferenceResponse>
            >(new GetPersonPreferencesQuery(userA, KeyPrefix: "grid-campus"))
        ).Value;
        var pref = Assert.Single(prefs);
        Assert.Equal("100", pref.Value);
        var deletedPref = await commands.SendAsync<DeletePersonPreferenceCommand, bool>(
            new DeletePersonPreferenceCommand(userA, "grid-campus-page-size")
        );
        Assert.True(deletedPref.IsSuccess);
    }
}
