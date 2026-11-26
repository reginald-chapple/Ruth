using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.Flows;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationFlowConversionConfiguration
    : IEntityTypeConfiguration<CommunicationFlowConversion>
{
    public void Configure(EntityTypeBuilder<CommunicationFlowConversion> builder)
    {
        _ = builder.ToTable("communication_flow_conversions");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FlowId).IsRequired();
        _ = builder.Property(static x => x.FlowInstanceId).IsRequired();
        _ = builder.Property(static x => x.FlowInstanceRecipientId).IsRequired();
        _ = builder
            .Property(static x => x.GoalKind)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.ConversionValue).HasPrecision(18, 2);
        _ = builder.Property(static x => x.ReferenceKey).HasMaxLength(300);
        _ = builder.Property(static x => x.Notes).HasMaxLength(500);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new { x.FlowInstanceRecipientId, x.GoalKind }).IsUnique();
        _ = builder.HasIndex(static x => x.FlowId);
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
