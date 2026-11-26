using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.VirtualRooms.Infrastructure.Configurations;

internal sealed class VirtualRoomConfiguration : IEntityTypeConfiguration<VirtualRoom>
{
    public void Configure(EntityTypeBuilder<VirtualRoom> builder)
    {
        _ = builder.ToTable("virtual_rooms");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.LiveKitRoomName).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.ContextKind).HasConversion<string>().HasMaxLength(40);
        _ = builder.Property(static x => x.Description).HasMaxLength(2000);
        _ = builder.Property(static x => x.ExternalReferenceKey).HasMaxLength(160);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => x.LiveKitRoomName).IsUnique();
        _ = builder.HasIndex(static x => x.ExternalReferenceKey);
        _ = builder.HasIndex(static x => x.LinkedGroupId);
        _ = builder.HasIndex(static x => x.LinkedBookingId);
    }
}
