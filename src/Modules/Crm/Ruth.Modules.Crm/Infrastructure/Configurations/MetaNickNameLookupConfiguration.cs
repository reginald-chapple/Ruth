using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class MetaNickNameLookupConfiguration : IEntityTypeConfiguration<MetaNickNameLookup>
{
    public void Configure(EntityTypeBuilder<MetaNickNameLookup> builder)
    {
        _ = builder.ToTable("meta_nick_name_lookups");
        _ = builder.HasKey(static m => m.Id);

        _ = builder.Property(static m => m.FirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.NormalizedFirstName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.NickName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.NormalizedNickName).HasMaxLength(50).IsRequired();
        _ = builder.Property(static m => m.GenderCode).HasMaxLength(1).IsRequired();

        _ = builder.HasIndex(static m => m.NormalizedFirstName);
        _ = builder.HasIndex(static m => m.NormalizedNickName);
    }
}
