using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Duplicates;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonRedirectConfiguration : IEntityTypeConfiguration<PersonRedirect>
{
    public void Configure(EntityTypeBuilder<PersonRedirect> builder)
    {
        _ = builder.ToTable("person_redirects");
        _ = builder.HasKey(static r => r.Id);

        _ = builder.HasIndex(static r => r.SourcePersonId).IsUnique();
        _ = builder.HasIndex(static r => r.TargetPersonId);
        _ = builder.HasIndex(static r => r.LegacyPersonAliasGuid);
    }
}
