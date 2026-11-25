using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PhoneNumberConfiguration : IEntityTypeConfiguration<PhoneNumber>
{
    public void Configure(EntityTypeBuilder<PhoneNumber> builder)
    {
        _ = builder.ToTable("phone_numbers");
        _ = builder.HasKey(static p => p.Id);

        _ = builder.Property(static p => p.NumberType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static p => p.CountryCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static p => p.Number).HasMaxLength(20).IsRequired();
        _ = builder.Property(static p => p.E164Number).HasMaxLength(25).IsRequired();
        _ = builder.Property(static p => p.NumberFormatted).HasMaxLength(50).IsRequired();
        _ = builder.Property(static p => p.NumberReversed).HasMaxLength(20).IsRequired();
        _ = builder.Property(static p => p.Extension).HasMaxLength(20);
        _ = builder.Property(static p => p.Description).HasMaxLength(200);

        _ = builder.HasIndex(static p => p.PersonId);
        _ = builder.HasIndex(static p => p.Number);
        _ = builder.HasIndex(static p => p.NumberReversed);
        _ = builder.HasIndex(static p => p.E164Number);
    }
}
