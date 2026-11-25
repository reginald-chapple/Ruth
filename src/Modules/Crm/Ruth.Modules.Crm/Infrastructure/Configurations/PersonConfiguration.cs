using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        _ = builder.ToTable("people");
        _ = builder.HasKey(static p => p.Id);

        ConfigureEnumsAndScalars(builder);
        ConfigureCollectionsAndAttributes(builder);
        ConfigureIndexes(builder);
    }

    private static void ConfigureEnumsAndScalars(EntityTypeBuilder<Person> builder)
    {
        _ = builder.Property(static p => p.RecordType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static p => p.RecordStatus).HasConversion<string>().HasMaxLength(32);
        _ = builder
            .Property(static p => p.ConnectionStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder
            .Property(static p => p.AccountProtectionProfile)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static p => p.Gender).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static p => p.MaritalStatus).HasConversion<string>().HasMaxLength(16);
        _ = builder
            .Property(static p => p.AgeClassification)
            .HasConversion<string>()
            .HasMaxLength(16);
        _ = builder
            .Property(static p => p.EmailPreference)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder
            .Property(static p => p.CommunicationPreference)
            .HasConversion<string>()
            .HasMaxLength(32);

        _ = builder.Property(static p => p.Title).HasMaxLength(50);
        _ = builder.Property(static p => p.FirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NormalizedFirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NickName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NormalizedNickName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.MiddleName).HasMaxLength(50);
        _ = builder.Property(static p => p.LastName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NormalizedLastName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.Suffix).HasMaxLength(50);
        _ = builder.Property(static p => p.Email).HasMaxLength(75);
        _ = builder.Property(static p => p.NormalizedEmail).HasMaxLength(75);
        _ = builder.Property(static p => p.EmailNote).HasMaxLength(250);
        _ = builder.Property(static p => p.TopSignalColor).HasMaxLength(100);
        _ = builder.Property(static p => p.TopSignalIcon).HasMaxLength(100);
    }

    private static void ConfigureCollectionsAndAttributes(EntityTypeBuilder<Person> builder)
    {
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static p => p.Attributes,
            "attributes",
            createGinIndex: true
        );
        _ = builder.Ignore(static p => p.AttributeValues);
        _ = builder.Ignore(static p => p.FullName);
        _ = builder.Ignore(static p => p.FullNameReversed);
        _ = builder.Ignore(static p => p.FullNameFormal);
        _ = builder.Ignore(static p => p.Initials);

        _ = builder
            .HasMany(static p => p.PhoneNumbers)
            .WithOne()
            .HasForeignKey(static pn => pn.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static p => p.PreviousNames)
            .WithOne()
            .HasForeignKey(static pn => pn.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static p => p.SearchKeys)
            .WithOne()
            .HasForeignKey(static sk => sk.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .Navigation(static p => p.PhoneNumbers)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static p => p.PreviousNames)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static p => p.SearchKeys)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Person> builder)
    {
        _ = builder.HasIndex(static p => p.NormalizedEmail);
        _ = builder.HasIndex(static p => p.NormalizedFirstName);
        _ = builder.HasIndex(static p => p.NormalizedNickName);
        _ = builder.HasIndex(static p => p.NormalizedLastName);
        _ = builder.HasIndex(static p => new { p.LastName, p.FirstName });
        _ = builder.HasIndex(static p => p.RecordStatus);
        _ = builder.HasIndex(static p => p.PrimaryCampusId);
        _ = builder.HasIndex(static p => p.FirstName).HasMethod("gin").HasOperators("gin_trgm_ops");
        _ = builder.HasIndex(static p => p.NickName).HasMethod("gin").HasOperators("gin_trgm_ops");
        _ = builder.HasIndex(static p => p.LastName).HasMethod("gin").HasOperators("gin_trgm_ops");
        _ = builder.HasIndex(static p => p.Email).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
