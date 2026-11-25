using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal static class CrmModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyPeopleConfigurations(modelBuilder);
        ApplyHouseholdsAndRelationshipsConfigurations(modelBuilder);
        ApplyProtectionAndVerificationConfigurations(modelBuilder);
    }

    private static void ApplyPeopleConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new PersonConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PhoneNumberConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonPreviousNameConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonSearchKeyConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetaphoneEntryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetaFirstNameGenderLookupConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MetaNickNameLookupConfiguration());
    }

    private static void ApplyHouseholdsAndRelationshipsConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new HouseholdConfiguration());
        _ = modelBuilder.ApplyConfiguration(new HouseholdMemberConfiguration());
        _ = modelBuilder.ApplyConfiguration(new HouseholdLocationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new KnownRelationshipConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PeerNetworkEdgeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonDuplicateConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonRedirectConfiguration());
    }

    private static void ApplyProtectionAndVerificationConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new SignalTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonSignalConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BadgeDefinitionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssessmentTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AssessmentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new BackgroundCheckConfiguration());
        _ = modelBuilder.ApplyConfiguration(new IdentityVerificationCodeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new IdentityVerificationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonViewedConfiguration());
    }
}
