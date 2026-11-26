using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.Flows;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationFlowInstanceConfiguration
    : IEntityTypeConfiguration<CommunicationFlowInstance>
{
    public void Configure(EntityTypeBuilder<CommunicationFlowInstance> builder)
    {
        _ = builder.ToTable("communication_flow_instances");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.FlowId).IsRequired();
        _ = builder.Property(static x => x.CohortLabel).HasMaxLength(150).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.FlowId, x.Status });
    }
}
