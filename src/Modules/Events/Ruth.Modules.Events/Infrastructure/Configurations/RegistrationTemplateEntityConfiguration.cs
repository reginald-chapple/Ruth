using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationTemplateEntityConfiguration
    : IEntityTypeConfiguration<RegistrationTemplate>
{
    public void Configure(EntityTypeBuilder<RegistrationTemplate> builder)
    {
        _ = builder.ToTable("registration_templates");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.FinancialAccountId).IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.BaseCostPerRegistrant).HasPrecision(18, 2).IsRequired();
        _ = builder
            .Property(static x => x.MinimumInitialDepositPerRegistrant)
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder
            .Property(static x => x.PaymentModel)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        _ = builder
            .HasMany(static x => x.Forms)
            .WithOne()
            .HasForeignKey(static x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Forms).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Fees)
            .WithOne()
            .HasForeignKey(static x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(static x => x.Fees).UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Discounts)
            .WithOne()
            .HasForeignKey(static x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Discounts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.GroupPlacements)
            .WithOne()
            .HasForeignKey(static x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.GroupPlacements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.IsActive);
    }
}
