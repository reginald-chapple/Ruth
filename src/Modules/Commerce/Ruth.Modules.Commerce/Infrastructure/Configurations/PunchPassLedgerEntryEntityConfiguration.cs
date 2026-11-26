using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.PunchPasses;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class PunchPassLedgerEntryEntityConfiguration
    : IEntityTypeConfiguration<PunchPassLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PunchPassLedgerEntry> builder)
    {
        _ = builder.ToTable("punch_pass_ledger_entries");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Id).ValueGeneratedNever();
        _ = builder
            .Property(static x => x.EntryKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.ReferenceNotes).HasMaxLength(500);
        _ = builder.HasIndex(static x => x.IssuedPunchPassId);
    }
}
