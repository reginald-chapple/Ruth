using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreCollaborationDomainTests
{
    [Fact]
    public void HistoryLogEntry_Masks_Sensitive_Values_And_Formats_Summaries()
    {
        var etId = EntityTypeDescriptorId.Create();
        var now = DateTimeOffset.UtcNow;

        var sensitive = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Modify",
                "Property",
                "Person",
                "SSN",
                "123-45-6789",
                "987-65-4321",
                isSensitive: true,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                "UI",
                changedByUserId: null,
                now
            )
            .Value;

        Assert.Null(sensitive.OldValue);
        Assert.Null(sensitive.NewValue);
        Assert.Contains(
            HistoryLogEntry.SensitiveMaskSummary,
            sensitive.FormatSummary(),
            StringComparison.Ordinal
        );

        var normal = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Modify",
                "Property",
                "Campus",
                "Name",
                "Old Campus",
                "New Campus",
                isSensitive: false,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                "API",
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Equal("Modified Name from 'Old Campus' to 'New Campus'.", normal.FormatSummary());

        var added = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Add",
                "Property",
                "Campus",
                "ShortCode",
                oldValue: null,
                "MAIN",
                isSensitive: false,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                sourceOfChange: null,
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Equal("Added ShortCode 'MAIN'.", added.FormatSummary());

        var removed = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Delete",
                "Property",
                "Campus",
                "ShortCode",
                "MAIN",
                newValue: null,
                isSensitive: false,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                sourceOfChange: null,
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Equal("Removed ShortCode 'MAIN'.", removed.FormatSummary());

        var captionOnly = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Login",
                "Record",
                "Admin User",
                valueName: null,
                oldValue: null,
                newValue: null,
                isSensitive: false,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                sourceOfChange: null,
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Equal("LOGIN Admin User", captionOnly.FormatSummary());

        var verbOnly = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Merge",
                "Record",
                caption: null,
                valueName: null,
                oldValue: null,
                newValue: null,
                isSensitive: false,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                sourceOfChange: null,
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Equal("MERGE", verbOnly.FormatSummary());

        var sensitiveNoField = HistoryLogEntry
            .Create(
                etId,
                Guid.CreateVersion7(),
                "Modify",
                "Property",
                caption: null,
                valueName: null,
                "a",
                "b",
                isSensitive: true,
                categoryId: null,
                relatedEntityTypeId: null,
                relatedEntityId: null,
                relatedData: null,
                sourceOfChange: null,
                changedByUserId: null,
                now
            )
            .Value;
        Assert.Contains(
            HistoryLogEntry.SensitiveMaskSummary,
            sensitiveNoField.FormatSummary(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Note_Visibility_Modification_And_NoteWatch_CoversScope_Handle_All_Branches()
    {
        var authorId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();
        var entityTypeId = EntityTypeDescriptorId.Create();
        var entityId = Guid.CreateVersion7();
        var noteType = NoteType.Create(entityTypeId, "General", requiresApprovals: true).Value;

        var pendingPublic = Note.Create(
            noteType,
            entityId,
            "Needs review",
            "Cap",
            parentNoteId: null,
            isAlert: false,
            isPinned: false,
            isPrivateNote: false,
            authorId,
            DateTimeOffset.UtcNow
        ).Value;
        Assert.True(pendingPublic.IsVisibleTo(authorId));
        Assert.False(pendingPublic.IsVisibleTo(otherId));
        Assert.True(pendingPublic.IsVisibleTo(otherId, includeUnapproved: true));
        Assert.True(pendingPublic.CanBeModifiedBy(otherId, isAdmin: true));
        Assert.True(pendingPublic.CanBeModifiedBy(authorId, isAdmin: false));
        Assert.False(pendingPublic.CanBeModifiedBy(otherId, isAdmin: false));
        Assert.False(pendingPublic.CanBeModifiedBy(requestingUserId: null, isAdmin: false));

        pendingPublic.SetApprovalStatus(NoteApprovalState.Denied, DateTimeOffset.UtcNow);
        Assert.False(pendingPublic.IsVisibleTo(authorId));

        pendingPublic.SetApprovalStatus(NoteApprovalState.Approved, DateTimeOffset.UtcNow);
        Assert.True(pendingPublic.IsVisibleTo(viewerUserId: null));

        var noteId = pendingPublic.Id;
        var byNote = NoteWatch
            .Create(
                authorId,
                isWatching: true,
                watchReplies: true,
                allowOverride: false,
                noteTypeId: null,
                entityTypeId: null,
                entityId: null,
                noteId
            )
            .Value;
        Assert.True(
            byNote.CoversScope(noteTypeId: null, entityTypeId: null, entityId: null, noteId)
        );
        Assert.False(
            byNote.CoversScope(
                noteTypeId: null,
                entityTypeId: null,
                entityId: null,
                NoteId.Create()
            )
        );
        Assert.False(
            byNote.CoversScope(noteTypeId: null, entityTypeId: null, entityId: null, noteId: null)
        );

        var byEntity = NoteWatch
            .Create(
                authorId,
                isWatching: true,
                watchReplies: true,
                allowOverride: false,
                noteType.Id,
                entityTypeId,
                entityId,
                noteId: null
            )
            .Value;
        Assert.True(byEntity.CoversScope(noteType.Id, entityTypeId, entityId, noteId: null));
        Assert.False(
            byEntity.CoversScope(NoteTypeId.Create(), entityTypeId, entityId, noteId: null)
        );
        Assert.False(
            byEntity.CoversScope(noteType.Id, entityTypeId, Guid.CreateVersion7(), noteId: null)
        );

        var byNoteType = NoteWatch
            .Create(
                authorId,
                isWatching: true,
                watchReplies: true,
                allowOverride: false,
                noteType.Id,
                entityTypeId: null,
                entityId: null,
                noteId: null
            )
            .Value;
        Assert.True(
            byNoteType.CoversScope(noteType.Id, entityTypeId: null, entityId: null, noteId: null)
        );
        Assert.False(
            byNoteType.CoversScope(
                NoteTypeId.Create(),
                entityTypeId: null,
                entityId: null,
                noteId: null
            )
        );

        var byEntityType = NoteWatch
            .Create(
                authorId,
                isWatching: true,
                watchReplies: true,
                allowOverride: false,
                noteTypeId: null,
                entityTypeId,
                entityId: null,
                noteId: null
            )
            .Value;
        Assert.True(
            byEntityType.CoversScope(noteTypeId: null, entityTypeId, entityId: null, noteId: null)
        );
        Assert.False(
            byEntityType.CoversScope(
                noteTypeId: null,
                EntityTypeDescriptorId.Create(),
                entityId: null,
                noteId: null
            )
        );
    }

    [Fact]
    public void Note_Enforces_Private_Author_Isolation_AutoCaption_And_Approval_State()
    {
        var authorId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();
        var noteType = NoteType
            .Create(EntityTypeDescriptorId.Create(), "Pastoral", requiresApprovals: true)
            .Value;

        var privateNote = Note.Create(
            noteType,
            Guid.CreateVersion7(),
            "Confidential reflection",
            caption: null,
            parentNoteId: null,
            isAlert: false,
            isPinned: false,
            isPrivateNote: true,
            authorUserId: authorId,
            nowUtc: DateTimeOffset.UtcNow
        ).Value;

        Assert.Equal(Note.PersonalNoteDefaultCaption, privateNote.Caption);
        Assert.Equal(NoteApprovalState.PendingApproval, privateNote.ApprovalStatus);
        Assert.True(privateNote.IsVisibleTo(authorId));
        Assert.False(privateNote.IsVisibleTo(otherUserId, includeUnapproved: true));
        Assert.True(privateNote.CanBeModifiedBy(authorId, isAdmin: false));
        Assert.False(privateNote.CanBeModifiedBy(otherUserId, isAdmin: true));
    }

    [Fact]
    public void Tag_EntitySet_And_PersonPreference_Enforce_Invariants()
    {
        Assert.True(Tag.Create("Bad<Tag>").IsFailure);
        Assert.True(Tag.Create("Bad&Tag").IsFailure);

        var owner = Guid.CreateVersion7();
        var personalTag = Tag.Create("My VIPs", ownerUserId: owner).Value;
        Assert.True(personalTag.IsPersonal);
        Assert.True(personalTag.IsAccessibleBy(owner));
        Assert.False(personalTag.IsAccessibleBy(Guid.CreateVersion7()));
        Assert.False(personalTag.IsAccessibleBy(null));

        var now = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var ephemeral = EntitySet
            .Create("Temp", entityTypeId: null, [Guid.CreateVersion7()], expireInMinutes: 15, now)
            .Value;
        Assert.False(ephemeral.IsExpiredAt(now.AddMinutes(10)));
        Assert.True(ephemeral.IsExpiredAt(now.AddMinutes(20)));

        var standardPref = PersonPreference
            .Create(
                owner,
                "grid-page-size",
                "25",
                entityTypeId: null,
                entityId: null,
                isEnduring: false,
                now
            )
            .Value;
        Assert.False(standardPref.IsExpiredRetention(now.AddMonths(1)));
        Assert.True(standardPref.IsExpiredRetention(now.AddMonths(3)));

        var enduringPref = PersonPreference
            .Create(
                owner,
                "theme",
                "dark",
                entityTypeId: null,
                entityId: null,
                isEnduring: true,
                now
            )
            .Value;
        Assert.False(enduringPref.IsExpiredRetention(now.AddMonths(12)));
        Assert.True(enduringPref.IsExpiredRetention(now.AddMonths(19)));
    }
}
