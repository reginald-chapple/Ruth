using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.WalletPasses;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class WalletPassTemplateEntityConfiguration
    : IEntityTypeConfiguration<WalletPassTemplate>
{
    public void Configure(EntityTypeBuilder<WalletPassTemplate> builder)
    {
        _ = builder.ToTable("wallet_pass_templates");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemGuid).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.PassTypeIdentifier).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.OrganizationName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(300).IsRequired();
        _ = builder
            .Property(static x => x.CardStyle)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.BackgroundColorHex).HasMaxLength(7).IsRequired();
        _ = builder.Property(static x => x.ForegroundColorHex).HasMaxLength(7).IsRequired();
        _ = builder.Property(static x => x.LabelColorHex).HasMaxLength(7).IsRequired();
        _ = builder.Property(static x => x.LogoText).HasMaxLength(80);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.Key).IsUnique();
        _ = builder.HasIndex(static x => x.SystemGuid).IsUnique();
    }
}
