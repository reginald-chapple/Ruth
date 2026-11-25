using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Core.Infrastructure.Configurations;

internal static class CoreCollaborationConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new BinaryFileTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BinaryFileConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DocumentTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DocumentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SignatureDocumentTemplateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SignatureDocumentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new HistoryLogEntryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new NoteTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new NoteConfiguration());
        _ = modelBuilder.ApplyConfiguration(new NoteAttachmentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new NoteWatchConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TagConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TaggedItemConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EntitySetConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EntitySetItemConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonPreferenceConfiguration());
    }
}
