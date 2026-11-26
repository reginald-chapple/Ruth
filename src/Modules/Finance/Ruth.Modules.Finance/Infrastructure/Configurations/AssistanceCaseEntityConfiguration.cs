using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Finance.Infrastructure.Configurations;

internal sealed class AssistanceCaseEntityConfiguration : IEntityTypeConfiguration<AssistanceCase>
{
    public void Configure(EntityTypeBuilder<AssistanceCase> builder)
    {
        _ = builder.ToTable("assistance_cases");
        _ = builder.HasKey(static x => x.Id);

        _ = builder.Property(static x => x.CaseNumber).HasMaxLength(40).IsRequired();
        _ = builder.Property(static x => x.ProgramId).IsRequired();
        _ = builder.Property(static x => x.ApplicantPersonId).IsRequired();
        _ = builder
            .Property(static x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.RequestDetails).HasMaxLength(2000);
        _ = builder.Property(static x => x.DecisionNotes).HasMaxLength(2000);
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.RequestedAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.ApprovedAmount).HasPrecision(18, 3).IsRequired();
        _ = builder.Property(static x => x.DisbursedAmount).HasPrecision(18, 3).IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder
            .HasMany(static x => x.Documents)
            .WithOne()
            .HasForeignKey(static x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Disbursements)
            .WithOne()
            .HasForeignKey(static x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Disbursements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.RemainingApprovedAmount);
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => x.CaseNumber).IsUnique();
        _ = builder.HasIndex(static x => new { x.ProgramId, x.Status });
        _ = builder.HasIndex(static x => x.ApplicantPersonId);
    }
}
