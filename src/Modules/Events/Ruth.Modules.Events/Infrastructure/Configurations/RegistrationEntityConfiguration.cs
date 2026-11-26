using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationEntityConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        _ = builder.ToTable("registrations");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.InstanceId).IsRequired();
        _ = builder.Property(static x => x.ConfirmationCode).HasMaxLength(32).IsRequired();
        _ = builder.Property(static x => x.RegistrarFirstName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.RegistrarLastName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.RegistrarEmail).HasMaxLength(254).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.AppliedDiscountCode).HasMaxLength(48);
        _ = builder.Property(static x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.DiscountTotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.PaidAmount).HasPrecision(18, 2).IsRequired();

        _ = builder
            .HasMany(static x => x.Registrants)
            .WithOne()
            .HasForeignKey(static x => x.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Registrants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Payments)
            .WithOne()
            .HasForeignKey(static x => x.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Payments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.BalanceDue);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.ConfirmationCode).IsUnique();
        _ = builder.HasIndex(static x => x.InstanceId);
        _ = builder.HasIndex(static x => x.RegistrarPersonId);
    }
}
