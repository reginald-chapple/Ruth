using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class IssuedWalletPassEntityConfiguration
    : IEntityTypeConfiguration<IssuedWalletPass>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ValueComparer<WalletPassDynamicFieldsDto> _fieldsComparer = new(
        static (left, right) =>
            string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal),
        static dto => StringComparer.Ordinal.GetHashCode(Serialize(dto)),
        static dto => Deserialize(Serialize(dto))
    );

    public void Configure(EntityTypeBuilder<IssuedWalletPass> builder)
    {
        _ = builder.ToTable("issued_wallet_passes");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.TemplateId).IsRequired();
        _ = builder.Property(static x => x.PersonId).IsRequired();
        _ = builder.Property(static x => x.SerialNumber).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.PassTypeIdentifier).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.ScanToken).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.AuthTokenSalt).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.AuthTokenHash).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.ClaimTokenHash).HasMaxLength(64).IsRequired();
        _ = builder
            .Property(static x => x.CardStyle)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.RevocationReason).HasMaxLength(240);

        _ = builder
            .Property(static x => x.DynamicFields)
            .HasColumnName("dynamic_fields")
            .HasColumnType("jsonb")
            .HasConversion(
                static dto => Serialize(dto),
                static json => Deserialize(json),
                _fieldsComparer
            )
            .IsRequired();

        _ = builder.Ignore(static x => x.UpdatedTag);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.SerialNumber).IsUnique();
        _ = builder.HasIndex(static x => x.ScanToken).IsUnique();
        _ = builder.HasIndex(static x => x.ClaimTokenHash).IsUnique();
        _ = builder.HasIndex(static x => new { x.PassTypeIdentifier, x.SerialNumber }).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
        _ = builder.HasIndex(static x => x.TemplateId);
    }

    private static string Serialize(WalletPassDynamicFieldsDto? dto)
    {
        return JsonSerializer.Serialize(dto ?? new WalletPassDynamicFieldsDto(), _jsonOptions);
    }

    private static WalletPassDynamicFieldsDto Deserialize(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new WalletPassDynamicFieldsDto()
            : JsonSerializer.Deserialize<WalletPassDynamicFieldsDto>(json, _jsonOptions)
                ?? new WalletPassDynamicFieldsDto();
    }
}
