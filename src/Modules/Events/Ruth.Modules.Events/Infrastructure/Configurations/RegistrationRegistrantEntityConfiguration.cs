using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Events.Infrastructure.Configurations;

internal sealed class RegistrationRegistrantEntityConfiguration
    : IEntityTypeConfiguration<RegistrationRegistrant>
{
    public void Configure(EntityTypeBuilder<RegistrationRegistrant> builder)
    {
        _ = builder.ToTable("registration_registrants");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.RegistrationId).IsRequired();
        _ = builder.Property(static x => x.InstanceId).IsRequired();
        _ = builder.Property(static x => x.FirstName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.LastName).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Email).HasMaxLength(254);
        _ = builder.Property(static x => x.Phone).HasMaxLength(40);
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.BaseCost).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.FeesTotal).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.NetCost).HasPrecision(18, 2).IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.FieldValues,
            columnName: "field_values",
            createGinIndex: true
        );

        _ = builder
            .HasMany(static x => x.FeeSelections)
            .WithOne()
            .HasForeignKey(static x => x.RegistrantId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.FeeSelections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.IsOnWaitlist);

        _ = builder.HasIndex(static x => new
        {
            x.InstanceId,
            x.Status,
            x.WaitlistPosition,
        });
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
