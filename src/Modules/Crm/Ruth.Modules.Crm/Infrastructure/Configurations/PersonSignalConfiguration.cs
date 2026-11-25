using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Signals;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PersonSignalConfiguration : IEntityTypeConfiguration<PersonSignal>
{
    public void Configure(EntityTypeBuilder<PersonSignal> builder)
    {
        _ = builder.ToTable("person_signals");
        _ = builder.HasKey(static s => s.Id);

        _ = builder.Property(static s => s.Note).HasMaxLength(2000);

        _ = builder.HasIndex(static s => s.PersonId);
        _ = builder.HasIndex(static s => s.SignalTypeId);
    }
}
