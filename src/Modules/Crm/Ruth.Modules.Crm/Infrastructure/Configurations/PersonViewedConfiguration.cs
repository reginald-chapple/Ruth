using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Verification;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonViewedConfiguration : IEntityTypeConfiguration<PersonViewed>
{
    public void Configure(EntityTypeBuilder<PersonViewed> builder)
    {
        _ = builder.ToTable("person_viewed");
        _ = builder.HasKey(static v => v.Id);

        _ = builder.Property(static v => v.IpAddress).HasMaxLength(45);
        _ = builder.Property(static v => v.Source).HasMaxLength(50);

        _ = builder.HasIndex(static v => new { v.TargetPersonId, v.ViewedOnUtc });
    }
}
