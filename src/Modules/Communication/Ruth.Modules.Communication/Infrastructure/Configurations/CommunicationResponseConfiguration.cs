using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Communication.Domain.TwoWaySms;

namespace Ruth.Modules.Communication.Infrastructure.Configurations;

internal sealed class CommunicationResponseConfiguration
    : IEntityTypeConfiguration<CommunicationResponse>
{
    public void Configure(EntityTypeBuilder<CommunicationResponse> builder)
    {
        _ = builder.ToTable("communication_responses");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.SystemPhoneNumberId).IsRequired();
        _ = builder.Property(static x => x.SystemPhoneNumber).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.ParticipantPhoneNumber).HasMaxLength(24).IsRequired();
        _ = builder.Property(static x => x.ParticipantDisplayName).HasMaxLength(200).IsRequired();
        _ = builder
            .Property(static x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        _ = builder.Property(static x => x.MessageBody).HasMaxLength(1_600).IsRequired();
        _ = builder.Property(static x => x.MatchedKeyword).HasMaxLength(80);
        _ = builder.Property(static x => x.ProviderMessageId).HasMaxLength(200);

        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new
        {
            x.SystemPhoneNumberId,
            x.ParticipantPhoneNumber,
            x.OccurredAtUtc,
        });
        _ = builder.HasIndex(static x => new
        {
            x.IsRead,
            x.Direction,
            x.OccurredAtUtc,
        });
        _ = builder.HasIndex(static x => x.ParticipantPersonId);
    }
}
