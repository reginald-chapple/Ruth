using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Core.Infrastructure;

/// <summary>EF Core database context for the Core module schema.</summary>
public sealed class CoreDbContext(DbContextOptions<CoreDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Core module.</summary>
    public const string SchemaName = "core";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the compile-time discovered entity type descriptors table.</summary>
    public DbSet<EntityTypeDescriptor> EntityTypeDescriptors => Set<EntityTypeDescriptor>();

    /// <summary>Gets the DefinedTypes lookup table.</summary>
    public DbSet<DefinedType> DefinedTypes => Set<DefinedType>();

    /// <summary>Gets the DefinedValues lookup items table.</summary>
    public DbSet<DefinedValue> DefinedValues => Set<DefinedValue>();

    /// <summary>Gets the hierarchical categories table.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Gets the attribute definitions table.</summary>
    public DbSet<AttributeDefinition> AttributeDefinitions => Set<AttributeDefinition>();

    /// <summary>Gets the attribute matrix templates table.</summary>
    public DbSet<AttributeMatrixTemplate> AttributeMatrixTemplates =>
        Set<AttributeMatrixTemplate>();

    /// <summary>Gets the branches (operating sites/chapters/centers) table.</summary>
    public DbSet<Branch> Branches => Set<Branch>();

    /// <summary>Gets the PostGIS-enabled locations table.</summary>
    public DbSet<Location> Locations => Set<Location>();

    /// <summary>Gets the physical devices table.</summary>
    public DbSet<Device> Devices => Set<Device>();

    /// <summary>Gets the schedules table.</summary>
    public DbSet<Schedule> Schedules => Set<Schedule>();

    /// <summary>Gets the materialized schedule dates horizon table.</summary>
    public DbSet<ScheduleDate> ScheduleDates => Set<ScheduleDate>();

    /// <summary>Gets the schedule category exclusions table.</summary>
    public DbSet<ScheduleCategoryExclusion> ScheduleCategoryExclusions =>
        Set<ScheduleCategoryExclusion>();

    /// <summary>Gets the binary file types table.</summary>
    public DbSet<BinaryFileType> BinaryFileTypes => Set<BinaryFileType>();

    /// <summary>Gets the binary files metadata table.</summary>
    public DbSet<BinaryFile> BinaryFiles => Set<BinaryFile>();

    /// <summary>Gets the document types table.</summary>
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    /// <summary>Gets the documents table.</summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>Gets the electronic signature document templates table.</summary>
    public DbSet<SignatureDocumentTemplate> SignatureDocumentTemplates =>
        Set<SignatureDocumentTemplate>();

    /// <summary>Gets the electronic signature documents table.</summary>
    public DbSet<SignatureDocument> SignatureDocuments => Set<SignatureDocument>();

    /// <summary>Gets the structured history log entries table.</summary>
    public DbSet<HistoryLogEntry> HistoryLogEntries => Set<HistoryLogEntry>();

    /// <summary>Gets the note types table.</summary>
    public DbSet<NoteType> NoteTypes => Set<NoteType>();

    /// <summary>Gets the threaded notes table.</summary>
    public DbSet<Note> Notes => Set<Note>();

    /// <summary>Gets the note watches table.</summary>
    public DbSet<NoteWatch> NoteWatches => Set<NoteWatch>();

    /// <summary>Gets the personal and organizational tags table.</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>Gets the tagged item associations table.</summary>
    public DbSet<TaggedItem> TaggedItems => Set<TaggedItem>();

    /// <summary>Gets the entity selection sets table.</summary>
    public DbSet<EntitySet> EntitySets => Set<EntitySet>();

    /// <summary>Gets the user/person preferences table.</summary>
    public DbSet<PersonPreference> PersonPreferences => Set<PersonPreference>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasPostgresExtension("postgis");
        CoreModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
