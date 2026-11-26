using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MobileApplicationConfiguration : IEntityTypeConfiguration<MobileApplication>
{
    public void Configure(EntityTypeBuilder<MobileApplication> builder)
    {
        _ = builder.ToTable("mobile_applications");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.BundleIdentifier).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.ApiVersion).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.MinimumClientVersion).HasMaxLength(32).IsRequired();
        _ = builder.Property(static x => x.LatestClientVersion).HasMaxLength(32).IsRequired();
        _ = builder.Property(static x => x.NavigationType).HasConversion<string>().HasMaxLength(24);
        _ = builder.Property(static x => x.PrimaryColorHex).HasMaxLength(16).IsRequired();
        _ = builder.Property(static x => x.SecondaryColorHex).HasMaxLength(16).IsRequired();
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AdditionalSettings,
            "additional_settings",
            createGinIndex: false
        );
        _ = builder
            .HasMany(static x => x.Layouts)
            .WithOne()
            .HasForeignKey(static l => l.MobileApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static x => x.Pages)
            .WithOne()
            .HasForeignKey(static p => p.MobileApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static x => x.DeepLinks)
            .WithOne()
            .HasForeignKey(static d => d.MobileApplicationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Layouts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Navigation(static x => x.Pages).UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static x => x.DeepLinks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
